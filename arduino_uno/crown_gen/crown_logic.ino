#include "crown_logic.h"

static const char MAGIC[] = "BGSASTNHOD01A02I";

static void reverse(uint8_t *arr, int len)
{
  for (int i = 0, j = len - 1; i < j; ++i, --j) {
    uint8_t x = arr[i];
    arr[i] = arr[j];
    arr[j] = x;
  }
}

void generate_security_sector(uint8_t *blob, uint8_t *cid, uint8_t *csd, uint8_t *ssr)
{
  randomSeed(analogRead(0));
  for (int i = 0; i < 512; ++i) {
    blob[i] = (uint8_t)random(256);
  }

  reverse(cid, 16);
  reverse(csd, 16);

  int start_offset = cid[0];

  for (int i = 0; i < sizeof(MAGIC) - 1; ++i) {
    blob[start_offset + i] = MAGIC[i] ^ cid[i];
  }

  uint16_t ssr_sum = 0;
  for (int i = 2; i < 14; ++i) {
    ssr_sum += ssr[i];
  }

  blob[start_offset + 0x10] = (uint8_t)(ssr_sum ^ cid[0]);
  blob[start_offset + 0x11] = (uint8_t)((ssr_sum >> 8) ^ csd[0]);
  blob[start_offset + 0x12] = cid[0] ^ csd[0];
}

int32_t get_crown_lba(Sd2Card *card, uint8_t *sector_buffer)
{
  // 1. Check that the disk is not GPT
  if (!card->readBlock(1, sector_buffer)) return -ERR_REG_READ_ERROR;
  if (!strncmp((char *)sector_buffer, "EFI PART", 8)) return -ERR_IS_GPT;

  // We should also check whether this is MBR, but strictly speaking the 55aa
  // marker is not required for non-bootable disks

  // 2. Find offset of end of first partition. We'll allow multiple partitions
  // only as long as there's enough of a gap to fit Code Crown data. If the
  // first partition stretches all the way to the end of disk, also fail.
  if (!card->readBlock(0, sector_buffer)) return -ERR_REG_READ_ERROR;
  mbr_t *mbr = (mbr_t *)sector_buffer;
  if (mbr->part[0].type == 0) return -ERR_NO_FIRST_PART;

  struct partition_range partitions[4] = {0};
  int part_i = 0;
  for (int i = 0; i < 4; ++i) {
    if (mbr->part[i].type == 0) continue;
    partitions[part_i].start_lba = mbr->part[i].firstSector;
    partitions[part_i].next_lba = mbr->part[i].firstSector + mbr->part[i].totalSectors;
    ++part_i;
  }

  // 3. Check for free space. Requires 1MB + 512 byte = 0x801 sectors of space.
  // Strictly speaking only 0xe0200 bytes are needed, but downloader will use
  // full 1MB
  if (part_i == 1) {
    csd_t *csd = (csd_t *)sector_buffer;
    if (!card->readCSD(csd)) return -ERR_REG_READ_ERROR;
    // Assume CSDv1 because caller checked
    uint16_t c_size = (csd->v1.c_size_high << 10) | (csd->v1.c_size_mid << 2) | csd->v1.c_size_low;
    uint8_t c_size_mult = (csd->v1.c_size_mult_high << 1) | csd->v1.c_size_mult_low;
    uint32_t end_lba = (c_size + 1) * (1 << (c_size_mult + 2)) * (1 << csd->v1.read_bl_len);
    if (end_lba - partitions[0].next_lba < 0x801)
      return -ERR_NO_SPACE;
  } else {
    if (partitions[1].start_lba - partitions[0].next_lba < 0x801)
      return -ERR_NO_SPACE;
  }
  return partitions[0].next_lba;
}
