#ifndef CROWN_LOGIC_H
#define CROWN_LOGIC_H

#include <stdint.h>

struct partition_range {
  uint32_t start_lba;
  uint32_t next_lba;
};

enum status {
  ERR_OK,
  ERR_REG_READ_ERROR,
  ERR_IS_GPT,
  ERR_NO_FIRST_PART,
  ERR_NO_SPACE,
  ERR_EIO,
  ERR_CSD_VER,
  ERR_CARD_INIT,
};

void generate_security_sector(uint8_t *blob, uint8_t *cid, uint8_t *csd, uint8_t *ssr);
int32_t get_crown_lba(Sd2Card *card);

#endif // CROWN_LOGIC_H
