import { createHmac } from 'node:crypto';

export function generateSignature(
  method: string,
  uri: string,
  timestamp: string,
  body: string,
  secret: string,
): string {
  const payload = `${method}\n${uri}\n${timestamp}\n${body}`;
  return createHmac('sha256', secret).update(payload, 'utf8').digest('base64');
}
