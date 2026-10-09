'use strict';

const { createHmac } = require('node:crypto');

function generateSignature(method, uri, timestamp, body, secret) {
  const payload = `${method}\n${uri}\n${timestamp}\n${body}`;
  return createHmac('sha256', secret).update(payload, 'utf8').digest('base64');
}

module.exports = { generateSignature };
