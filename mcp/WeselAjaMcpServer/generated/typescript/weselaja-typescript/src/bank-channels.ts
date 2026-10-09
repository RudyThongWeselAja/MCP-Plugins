const channels: Readonly<Record<string, string>> = {
  BRI: 'BRI.VA',
  BANKBRI: 'BRI.VA',
  MANDIRI: 'MDR.VA',
  BANKMANDIRI: 'MDR.VA',
  MDR: 'MDR.VA',
  BNI: 'BNI.VA',
  BANKBNI: 'BNI.VA',
  PERMATA: 'PTB.VA',
  BANKPERMATA: 'PTB.VA',
  PTB: 'PTB.VA',
  CIMB: 'CIMB.VA',
  CIMBNIAGA: 'CIMB.VA',
  BANKCIMB: 'CIMB.VA',
  BCA: 'BCA.VA',
  BANKBCA: 'BCA.VA',
};

export function resolveBankChannel(bank: string): string {
  if (bank.trim() === '') {
    throw new TypeError('Bank is required.');
  }

  const normalized = bank.trim().toUpperCase().replace(/[ _-]/g, '');
  const channel = channels[normalized];
  if (!channel) {
    throw new RangeError(
      `Unsupported bank '${bank}'. Supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA.`,
    );
  }
  return channel;
}

export function supportedBanks(): Readonly<Record<string, string>> {
  return {
    BRI: 'BRI.VA',
    Mandiri: 'MDR.VA',
    BNI: 'BNI.VA',
    Permata: 'PTB.VA',
    CIMB: 'CIMB.VA',
    BCA: 'BCA.VA',
  };
}
