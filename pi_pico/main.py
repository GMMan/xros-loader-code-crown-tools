from binascii import hexlify
import machine
import os
import sdcard
import struct
import time

# Hardware definitions ========================================================

# LEDs
led_green = machine.Pin(25, machine.Pin.OUT)
led_red = machine.Pin(4, machine.Pin.OUT)

# Card detect
card_detect = machine.Pin(20, machine.Pin.IN, machine.Pin.PULL_UP)

# SPI
spi_sck = machine.Pin(18)
spi_mosi = machine.Pin(19)
spi_miso = machine.Pin(16)
spi_cs = machine.Pin(17, machine.Pin.OUT)

spi = machine.SPI(0, baudrate=2_000_000, sck=spi_sck, mosi=spi_mosi,
                  miso=spi_miso)

# Main logic ==================================================================

LED_BLINK_TIME_MS = 250

MAGIC = b'BGSASTNHOD01A02I'
GPT_MAGIC = b'EFI PART'

ERR_OK = 0
ERR_REG_READ_ERROR = 1
ERR_IS_GPT = 2
ERR_NO_FIRST_PART = 3
ERR_NO_SPACE = 4
ERR_EIO = 5
ERR_CSD_VER = 6

# https://forum.micropython.org/viewtopic.php?t=1420#p8788
@micropython.asm_thumb
def reverse(r0, r1):               # bytearray, len(bytearray)
    add(r4, r0, r1)
    sub(r4, 1) # end address
    label(LOOP)
    ldrb(r5, [r0, 0])
    ldrb(r6, [r4, 0])
    strb(r6, [r0, 0])
    strb(r5, [r4, 0])
    add(r0, 1)
    sub(r4, 1)
    cmp(r4, r0)
    bpl(LOOP)


def generate_security_sector(cid, csd, ssr):
    blob = bytearray(os.urandom(512))

    reverse(cid, 16)
    reverse(csd, 16)

    start_offset = cid[0]

    for i in range(len(MAGIC)):
        blob[start_offset + i] = MAGIC[i] ^ cid[i]
    
    ssr_sum = 0
    for i in range(2, 14):
        ssr_sum += ssr[i]
    ssr_sum &= 0xffff

    blob[start_offset + 0x10] = (ssr_sum ^ cid[0]) & 0xff
    blob[start_offset + 0x11] = ((ssr_sum >> 8) ^ csd[0]) & 0xff
    blob[start_offset + 0x12] = cid[0] ^ csd[0]

    return blob


def get_crown_lba(card):
    sector_buffer = bytearray(512)

    # Note: block numbers are in bytes for SDCard driver, not sectors!
    block_size = card.ioctl(5, None)

    # 1. Check that the disk is not GPT
    card.readblocks(1 * (512 // block_size), sector_buffer)
    if sector_buffer == GPT_MAGIC:
        print('Cannot process GPT disks.')
        return -ERR_IS_GPT
    
    # We should also check whether this is MBR, but strictly speaking the 55aa
    # marker is not required for non-bootable disks

    # 2. Find offset of end of first partition. We'll allow multiple partitions
    # only as long as there's enough of a gap to fit Code Crown data. If the
    # first partition stretches all the way to the end of disk, also fail.
    card.readblocks(0 * (512 // block_size), sector_buffer)
    if sector_buffer[0x1be + (0 * 16) + 4] == 0:
        print('First partition cannot be free.')
        return -ERR_NO_FIRST_PART

    partitions = []
    for i in range(4):
        if sector_buffer[0x1be + (i * 16) + 4] == 0:
            continue

        offset = 0x1be + (i * 16) + 8
        start_lba = struct.unpack('<I', sector_buffer[offset:offset + 4])[0]
        offset += 4
        sector_length = struct.unpack('<I', sector_buffer[offset:offset + 4])[0]
        partitions.append((start_lba, start_lba + sector_length))
    
    # 3. Check for free space. Requires 1MB + 512 byte = 0x801 sectors of space.
    # Strictly speaking only 0xe0200 bytes are needed, but downloader will use
    # full 1MB
    if len(partitions) == 1:
        end_lba = card.ioctl(4, None) * block_size // 512
        if end_lba - partitions[0][1] < 0x801:
            print('Not enough space after first partition for Code Crown data.')
            return -ERR_NO_SPACE
        return partitions[0][1]
    else:
        if partitions[1][0] - partitions[0][1] < 0x801:
            print('Not enough space after first partition and before next partition for Code Crown data.')
            return -ERR_NO_SPACE
        return partitions[0][1]
 

def write_security_sector():
    print('Starting process')

    card = sdcard.SDCard(spi, spi_cs)
    if card.csd_version != 1:
        print('CSD is not v1, card will not work in Xros Loader.')
        return -ERR_CSD_VER

    cid = bytearray(16)
    csd = bytearray(16)
    ssr = bytearray(64)

    # Read CID
    if card.cmd(10, 0, 0, 0, False) != 0:
        spi_cs.value(1)
        print('Error getting CID')
        return -ERR_REG_READ_ERROR
    card.readinto(cid)

    # Read CSD
    if card.cmd(9, 0, 0, 0, False) != 0:
        spi_cs.value(1)
        print('Error getting CSD')
        return -ERR_REG_READ_ERROR
    card.readinto(csd)

    # Read SSR
    # Need to consume one extra byte for response type R2
    if card.cmd(55, 0, 0) != 0 or card.cmd(13, 0, 0, 1, False) != 0:
        spi_cs.value(1)
        print('Error getting SSR')
        return -ERR_REG_READ_ERROR
    card.readinto(ssr)

    print('CID: {}'.format(hexlify(cid)))
    print('CSD: {}'.format(hexlify(csd)))
    print('SSR: {}'.format(hexlify(ssr)))

    lba = get_crown_lba(card)
    if (lba < 0):
        return lba
    sec = generate_security_sector(cid, csd, ssr)
    card.writeblocks(lba * (512 // card.ioctl(5, None)), sec)
    
    print('Process complete')
    return ERR_OK


def blink_err(times):
    # Blink off
    for _ in range(times):
        led_red.value(0)
        time.sleep_ms(LED_BLINK_TIME_MS)
        led_red.value(1)
        time.sleep_ms(LED_BLINK_TIME_MS)


# Main loop ===================================================================

# Red LED on by default to indicate power
led_red.value(1)
led_green.value(0)

while True:
    # Wait until card is inserted (reads low)
    while card_detect.value() != 0:
        time.sleep_ms(50)

    # Light LED to indicate we are running
    led_green.value(1)
    print('Card inserted')
    # Wait a bit in case card is still being inserted
    time.sleep_ms(200)

    # Let's go!
    try:
        ret = write_security_sector()
    except Exception as ex:
        print('Exception caught: ' + str(ex))
        ret = -ERR_EIO

    # Done, extinguish LED
    led_green.value(0)

    ret = -ret
    if ret != ERR_OK:
        # Blink error until card removed
        while card_detect.value() == 0:
            blink_err(ret)
            time.sleep_ms(1000)
    else:
        # Wait until card is removed (reads high)
        while card_detect.value() == 0:
            time.sleep_ms(50)
    print('Card removed')
