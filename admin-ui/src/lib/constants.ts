/**
 * Tất cả text/config hardcoded tập trung ở đây.
 * Khi cần đổi text, chỉ sửa file này.
 */

// ── App Info ──
export const APP = {
  name: 'Integration Platform',
  shortName: 'IP',
  version: '1.0.0',
};

// ── Messages ──
export const MSG = {
  // Integration CRUD
  INTEGRATION_CREATED: 'Integration đã được tạo thành công',
  INTEGRATION_UPDATED: 'Integration đã được cập nhật',
  INTEGRATION_DELETED: 'Integration đã được xóa',
  INTEGRATION_ACTIVATED: 'Integration đã được kích hoạt',
  INTEGRATION_DEACTIVATED: 'Integration đã được tạm dừng',

  // Errors
  ERR_CREATE_INTEGRATION: 'Không thể tạo integration. Kiểm tra lại thông tin.',
  ERR_UPDATE_INTEGRATION: 'Không thể cập nhật integration.',
  ERR_DELETE_INTEGRATION: 'Không thể xóa integration.',
  ERR_LOAD_DATA: 'Không thể tải dữ liệu. Kiểm tra kết nối backend.',
  ERR_CONNECTION_TEST: 'Test kết nối thất bại.',
  ERR_NETWORK: 'Lỗi mạng. Kiểm tra backend đang chạy.',

  // Validation
  VAL_REQUIRED_NAME: 'Tên hiển thị là bắt buộc',
  VAL_REQUIRED_KEY: 'Config Key là bắt buộc',
  VAL_REQUIRED_URL: 'Base URL là bắt buộc',

  // Settings
  SETTINGS_SAVED: 'Cài đặt đã được lưu',

  // General
  CONFIRM_DELETE: 'Bạn có chắc muốn xóa? Hành động này không thể hoàn tác.',
  NO_DATA: 'Chưa có dữ liệu',
};

// ── Auth Types ──
export const AUTH_TYPES = [
  { value: 'none', label: 'Không (None)' },
  { value: 'apikey', label: 'API Key' },
  { value: 'basic', label: 'Basic Auth' },
  { value: 'bearer', label: 'Bearer Token' },
] as const;

// ── Status Labels ──
export const STATUS_LABELS: Record<string, string> = {
  active: 'Hoạt động',
  draft: 'Bản nháp',
  inactive: 'Tạm dừng',
  archived: 'Đã lưu trữ',
};

// ── Navigation ──
export const NAV_ITEMS = [
  { key: '/dashboard', label: 'Dashboard', desc: 'Tổng quan hệ thống' },
  { key: '/integrations', label: 'Integrations', desc: 'Quản lý cấu hình kết nối' },
  { key: '/executions', label: 'Executions', desc: 'Lịch sử gọi API' },
  { key: '/audit', label: 'Audit Logs', desc: 'Lịch sử thay đổi' },
  { key: '/settings', label: 'Settings', desc: 'Cài đặt hệ thống' },
] as const;

// ── Defaults ──
export const DEFAULTS = {
  timeout: 30000,
  pageSize: 20,
  connectorType: 'generic-http',
  status: 'draft' as const,
};

// ── Mock Execution Data ──
export const MOCK_EXECUTIONS = [
  { id: '1', configKey: 'bank-b-transfer', operation: 'transfer', status: 'success', duration: 245, time: '2 phút trước' },
  { id: '2', configKey: 'ewallet-payment', operation: 'payment', status: 'success', duration: 189, time: '5 phút trước' },
  { id: '3', configKey: 'bank-c-transfer', operation: 'transfer', status: 'failed', duration: 30012, time: '12 phút trước' },
  { id: '4', configKey: 'sms-gateway', operation: 'send-otp', status: 'success', duration: 98, time: '15 phút trước' },
  { id: '5', configKey: 'bank-b-transfer', operation: 'balance-inquiry', status: 'success', duration: 156, time: '20 phút trước' },
];

// ── Mock Audit Data ──
export const MOCK_AUDIT_LOGS = [
  { id: '1', action: 'Created', entity: 'bank-b-transfer', user: 'admin', time: '2024-01-15 10:30' },
  { id: '2', action: 'Activated', entity: 'bank-b-transfer', user: 'admin', time: '2024-01-15 10:35' },
  { id: '3', action: 'Created', entity: 'ewallet-payment', user: 'system', time: '2024-01-15 11:00' },
  { id: '4', action: 'Updated', entity: 'bank-c-transfer', user: 'admin', time: '2024-01-16 09:15' },
  { id: '5', action: 'Activated', entity: 'ewallet-payment', user: 'admin', time: '2024-01-16 09:20' },
];
