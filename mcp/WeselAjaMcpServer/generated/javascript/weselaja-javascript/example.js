'use strict';

const { WeselAjaClient } = require('./src');

async function main() {
  const client = WeselAjaClient.fromEnvironment();
  const payment = await client.createPayment({
    amount: 10000,
    customerName: 'Andi',
    bank: 'BNI',
    description: 'WeselAja sandbox example',
  });

  console.log('Payment created successfully.');
  console.log(`Status   : ${payment.status || '-'}`);
  console.log(`Reference: ${payment.referenceCode || '-'}`);
  console.log(`Channel  : ${payment.paymentChannel || '-'}`);
  console.log(`Payment  : ${payment.paymentCode || '-'}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
