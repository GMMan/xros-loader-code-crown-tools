Digimon Xros Loader Code Crown Generator for Raspberry Pi Pico
==============================================================

The firmware for Raspberry Pi Pico will convert a compatible SD card into a
Code Crown Zero.

Hardware connection
-------------------

### Bill of materials

- 1x Raspberry Pi Pico
- 1x SD card/microSD card breakout board (3.3v compatible)
- 1x Red LED
- 1x Resistor (for current limiting the LED; please calculate the value based
  on your LED's datasheet, but in general a 330Ω resistor will work)
- 1x Toggle switch or button if your breakout board does not have card detect
  broken out
- 1x Breadboard (optional)
- Hookup wires

### Hardware connection

1. Connect the `3V3` pin on the Pico to your positive power rail and any `GND`
   pin to your ground rail.
2. Connect the microSD card breakout to the Pico as follows:
   - Pico pin 21 to `DAT0` or `MISO`
   - Pico pin 22 to `DAT3` or `CS`
   - Pico pin 24 to `CLK` or `CLK`
   - Pico pin 25 to `CMD` or `MOSI`
   - Pico pin 26 to card detect. Note, if your breakout board does not have a
     card detect pin, you can connect a toggle switch or button to pin 26, and
     connect the other side to ground.
3. Connect the red LED such that the anode end has the Pico's pin 6 upstream.
   Connect a resistor before or after the LED, and connect the loose end of the
   LED or resistor to ground.

Software setup
--------------

1. Install MicroPython on to your Pi Pico
   - Download a recent build from [here](https://micropython.org/download/rp2-pico/)
   - Hold the `BOOTSEL` button on your Pico, then plug the Pico into your
     computer while still holding the button. You can release the button
     once the Pico shows up on your computer as a USB drive.
   - Drag the .uf2 firmware you have downloaded to the USB drive, and the
     Pico will install the firmware and reboot
2. Upload the code in this directory to the Pico
   - I use [Thonny](https://thonny.org/). After you install it, open it up
     and click the button at the bottom right of the screen. Select
     `MicroPython (Raspberry Pi Pico)`.
   - Open up each file in the firmware directory, and select `File ->
     Save copy...` from the menu strip. Click `Raspbery Pi Pico` when asked
     where to save to. Type in the name of the file in the `File name` box,
     and click `OK`.
   - Note `sdcard.py` is in a folder. Due to how search paths works, you can
     either put it into the root of the Pico, or create a new directory named
     `lib` and place it in there.
3. Replug the Pico and verify the code is installed
   - The red LED should be lit

Usage
-----

Simply insert your SD card into the slot. If you only have a microSD slot
available but you only have a full-size SD card, insert the SD card into the
SD to microSD adapter first, then insert the microSD end to the slot attached
to the Pico. The green LED on the Pico will light up while it's processing,
and extinguish after it's done. If the operation was successful, the red LED
will stay solid, otherwise it will blink off an error code:

- 1 blink: failed to read card registers
- 2 blinks: card is formatted GPT; it must be reformatted with a MBR partition
  table
- 3 blinks: card does not have a partition in the first partition slot; you
  must add a partition
- 4 blinks: there is not enough free space after the first partition; you need
  to shrink or repartition the first partition so there is over 1MB of
  unpartitioned space immediately after it
- 5 blinks: read/write error, replug the card to try again
- 6 blinks: CSD version mismatch: the Xros Loader will probably not recognize
  this card

More detailed error messages are printed to USB serial port. You can
also view the messages in Thonny.

If your SD breakout board does not have a
card detect pin, then you can use the switch/button alternative. Flip the
switch or hold the button after you have inserted a card. The process will
complete and the red LED will flash if there are errors. If you are using a
button, make sure to hold the button until the error has finished blinking, if
applicable. Flip the switch again or release the button before removing the
card.
