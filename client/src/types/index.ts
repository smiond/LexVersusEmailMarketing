export interface User {
  id: string;
  email: string;
  fullName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface UnsubscribeInfo {
  email: string;
  isSubscribed: boolean;
  unsubscribedAt?: string;
}

export interface UnsubscribeResult {
  success: boolean;
  message: string;
}

export interface Contact {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isSubscribed: boolean;
  createdAt: string;
}

export interface EmailList {
  id: string;
  name: string;
  description?: string;
  contactCount: number;
  createdAt: string;
}

export interface ImportResult {
  imported: number;
  skipped: number;
  duplicates: number;
  errors: string[];
}

export interface EmailTemplate {
  id: string;
  name: string;
  subject: string;
  htmlBody: string;
  createdAt: string;
  updatedAt: string;
}

export interface SenderIdentity {
  id: string;
  email: string;
  displayName: string;
  replyToEmail?: string;
  isDefault: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface Campaign {
  id: string;
  name: string;
  emailTemplateId: string;
  emailTemplateName: string;
  emailListId?: string;
  emailListName?: string;
  senderIdentityId?: string;
  senderEmail?: string;
  senderDisplayName?: string;
  status: string;
  subjectOverride?: string;
  scheduledAt?: string;
  sentAt?: string;
  createdAt: string;
  totalRecipients: number;
  sentCount: number;
  failedCount: number;
}

export interface CampaignSendResult {
  campaignId: string;
  status: string;
  sent: number;
  failed: number;
  total: number;
}

export interface DashboardStats {
  totalContacts: number;
  totalTemplates: number;
  totalCampaigns: number;
  activeCampaigns: number;
  emailsSentToday: number;
}
