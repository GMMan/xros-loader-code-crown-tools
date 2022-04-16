#include <SPI.h>
#include <SDMOD.h>
#include "crown_logic.h"

const int chipSelect = 10;
const int LED_BLINK_TIME_MS = 250;

static enum status g_status;

static void print_hex(uint8_t *buf, int len)
{
  for (int i = 0; i < len; ++i) {
    Serial.print(buf[i] >> 4, HEX);
    Serial.print(buf[i] & 0xf, HEX);
  }
  Serial.print("\n");
}

enum status write_security_sector(Sd2Card *card)
{
  uint8_t cid[16];
  uint8_t csd[16];
  uint8_t ssr[64];
  uint8_t sector[512];

  if (!card->readCID((cid_t *)cid)) return ERR_REG_READ_ERROR;
  if (!card->readCSD((csd_t *)csd)) return ERR_REG_READ_ERROR;
  if (!card->readSSR(ssr)) return ERR_REG_READ_ERROR;

  Serial.print("CID: ");
  print_hex(cid, sizeof(cid));
  Serial.print("CSD: ");
  print_hex(csd, sizeof(csd));
  Serial.print("SSR: ");
  print_hex(ssr, sizeof(ssr));

  if (((csd_t *)csd)->v1.csd_ver != 0) return ERR_CSD_VER;

  int32_t lba = get_crown_lba(card, sector);
  if (lba < 0) return (enum status)-lba;

  generate_security_sector(sector, cid, csd, ssr);
  if (!card->writeBlock((uint32_t)lba, sector)) return ERR_EIO;
  return ERR_OK;
}

void setup() {
  // put your setup code here, to run once:
  Sd2Card card;

  Serial.begin(9600);
  if (card.init(SPI_FULL_SPEED, chipSelect)) {
    Serial.println("Starting process");
    g_status = write_security_sector(&card);
    switch (g_status) {
      case ERR_OK:
        Serial.println("Process complete");
        break;
      case ERR_REG_READ_ERROR:
        Serial.println("Error getting card register.");
        Serial.print("Error code: ");
        Serial.println(card.errorCode());
        Serial.print("Error data: ");
        Serial.println(card.errorData());
        break;
      case ERR_IS_GPT:
        Serial.println("Cannot process GPT disks.");
        break;
      case ERR_NO_FIRST_PART:
        Serial.println("First partition cannot be free.");
        break;
      case ERR_NO_SPACE:
        Serial.println("Not enough space after first partition for Code Crown data.");
        break;
      case ERR_EIO:
        Serial.println("I/O error");
        break;
      case ERR_CSD_VER:
        Serial.println("CSD is not v1, card will not work in Xros Loader.");
        break;
    }
  } else {
    g_status = ERR_CARD_INIT;
    Serial.println("Could not communicate with card. Please check your connection.");
  }

  // Setup LED blinking
  SPI.end();
  pinMode(LED_BUILTIN, OUTPUT);
}

void loop() {
  // put your main code here, to run repeatedly:
  if (g_status == ERR_OK) {
    digitalWrite(LED_BUILTIN, HIGH);
  } else {
    for (int i = 0; i < (int)g_status; ++i) {
      digitalWrite(LED_BUILTIN, HIGH);
      delay(LED_BLINK_TIME_MS);
      digitalWrite(LED_BUILTIN, LOW);
      delay(LED_BLINK_TIME_MS);
    }
    delay(1000);
  }
}
