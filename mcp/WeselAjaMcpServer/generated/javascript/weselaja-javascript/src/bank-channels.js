'use strict';

const channels = new Map([
  ['BRI', 'BRI.VA'],
  ['BANKBRI', 'BRI.VA'],
  ['MANDIRI', 'MDR.VA'],
  ['BANKMANDIRI', 'MDR.VA'],
  ['MDR', 'MDR.VA'],
  ['BNI', 'BNI.VA'],
  ['BANKBNI', 'BNI.VA'],
  ['PERMATA', 'PTB.VA'],
  ['BANKPERMATA', 'PTB.VA'],
  ['PTB', 'PTB.VA'],
  ['CIMB', 'CIMB.VA'],
  ['CIMBNIAGA', 'CIMB.VA'],
  ['BANKCIMB', 'CIMB.VA'],
  ['BCA', 'BCA.VA'],
  ['BANKBCA', 'BCA.VA'],
]);

function resolveBankChannel(bank) {
  if (typeof bank !== 'string' || bank.trim() === '') {
    throw new TypeError('Bank is required.');
  }

  const normalized = bank.trim().toUpperCase().replace(/[ _-]/g, '');
  const channel = channels.get(normalized);
  if (!channel) {
    throw new RangeError(
      `Unsupported bank '${bank}'. Supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA.`,
    );
  }
  return channel;
}

function supportedBanks() {
  return {
    BRI: 'BRI.VA',
    Mandiri: 'MDR.VA',
    BNI: 'BNI.VA',
    Permata: 'PTB.VA',
    CIMB: 'CIMB.VA',
    BCA: 'BCA.VA',
  };
}

module.exports = { resolveBankChannel, supportedBanks };
