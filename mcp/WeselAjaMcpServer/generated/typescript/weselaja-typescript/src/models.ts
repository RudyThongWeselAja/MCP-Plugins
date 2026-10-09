export interface CreatePaymentRequest {
  amount: number;
  customerName: string;
  bank: string;
  customerPhoneNumber?: string;
  description?: string;
  referenceCode?: string;
}

export interface CreatePaymentResponse {
  success: boolean;
  paymentId?: string;
  initiatedAmount?: number;
  paymentAmount?: number | string;
  feeAmount?: number | string;
  currency?: string;
  paymentMethod?: string;
  paymentChannel?: string;
  paymentCode?: string;
  paymentCodeType?: string;
  referenceCode?: string;
  customerReference?: string;
  customerName?: string;
  status?: string;
  createdTime?: string;
  updatedTime?: string;
  expirationTime?: string;
  description?: string;
  callbackUrl?: string;
  redirectUrl?: string;
  payerAccountName?: string;
  payerAccountNumber?: string;
  payerPaymentChannel?: string;
  metadata?: Record<string, unknown>;
  [key: string]: unknown;
}

export interface ClientOptions {
  fetchImpl?: typeof fetch;
  now?: () => Date;
}
