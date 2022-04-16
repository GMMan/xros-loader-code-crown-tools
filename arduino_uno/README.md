Digimon Xros Loader Code Crown Generator for Arduino Uno
========================================================

This sketch for Arduino Uno will convert a compatible SD card into a Code
Crown Zero.

Hardware connection
-------------------

### Bill of materials

- 1x Arduino Uno
- 1x SD card/micro SD card breakout board (with level shifting) ([Amazon.ca](https://www.amazon.ca/gp/product/B07MTTLF75/))
  - Note: you should try to get one with a level shifter to reduce the
    likelihood of damaging the SD card's I/O pins and for better reliability.
- Hookup wires

### Hardware connection

Connect the pins between the Arduino and SD card breakout with hookup wires as
follows:

| SD card breakout | Arduino |
|------------------|---------|
| CS               | 10      |
| SCK              | 13      |
| MOSI             | 11      |
| MISO             | 12      |
| VCC              | 5V      |
| GND              | GND     |

Software setup
--------------

1. Install Arduino IDE. See [here](https://www.arduino.cc/en/Guide#install-the-arduino-desktop-ide)
   for instructions.
2. Copy the `SDMOD` folder in the [libraries](libraries) folder to your
   Arduino libraries folder. See [here](https://docs.arduino.cc/software/ide-v1/tutorials/installing-libraries#manual-installation)
   for instructions.
3. Open up the sketch in the [crown_gen](crown_gen) folder in Arduino IDE.
4. Connect your Arduino and select the correct board and port under the
   `Tools` menu.
5. Click the `Upload` button.

Usage
-----

Insert your SD card into the breakout board, then either connect the Arduino
to power, or if it's already powered, press the reset button. The code will
detect the card and write the security sector. If it was successful, the
on-board LED will stay lit. If it was not successful, it will blink out
an error code:

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
- 7 blinks: error initializing the card driver. Make sure you have connected
  your breakout board correctly.

More detailed error messages are printed to the serial port, which you can
view in Arduino IDE's serial monitor.
