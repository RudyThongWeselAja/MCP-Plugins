import { WeselAjaClient } from './src/index.js';

async function main(): Promise<void> {
  const client = await WeselAjaClient.fromEnvironment();
  const payment = await client.createPayment({
    amount: 10_000,
    customerName: 'Andi',
    bank: 'Permata',
    description: 'WeselAja sandbox example',
  });

  console.log('Payment created successfully.');
  console.log(`Status   : ${payment.status ?? '-'}`);
  console.log(`Reference: ${payment.referenceCode ?? '-'}`);
  console.log(`Channel  : ${payment.paymentChannel ?? '-'}`);
  console.log(`Payment  : ${payment.paymentCode ?? '-'}`);
}

main().catch((error: unknown) => {
  console.error(error);
  process.exitCode = 1;
});
