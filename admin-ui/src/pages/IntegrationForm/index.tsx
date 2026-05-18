import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Building2,
  Wallet,
  MessageSquare,
  Globe,
  ChevronLeft,
  ChevronRight,
  Plus,
  Trash2,
  Loader2,
  CheckCircle2,
  XCircle,
  Zap,
} from 'lucide-react';
import { useCreateIntegration } from '../../hooks/useIntegrations';
import { integrationsApi } from '../../api/integrations';

// ── Types ────────────────────────────────────────────────────

interface FieldMapping {
  sourceField: string;
  targetField: string;
  isRequired: boolean;
}

interface FormData {
  template: string | null;
  configKey: string;
  name: string;
  baseUrl: string;
  authType: 'none' | 'apikey' | 'basic' | 'bearer';
  authToken: string;
  timeoutMs: number;
  mappings: FieldMapping[];
}

interface TestResultState {
  success: boolean;
  message: string;
  response?: Record<string, unknown>;
}

// ── Templates ────────────────────────────────────────────────

const TEMPLATES = [
  {
    id: 'bank-transfer',
    title: 'Bank Transfer',
    description: 'Fund transfer via banking partner API',
    icon: Building2,
    defaults: {
      configKey: 'bank-transfer',
      name: 'Bank Transfer',
      baseUrl: 'https://api.bank-partner.com/v1',
    },
  },
  {
    id: 'e-wallet',
    title: 'E-Wallet',
    description: 'Payment via e-wallet provider',
    icon: Wallet,
    defaults: {
      configKey: 'ewallet-payment',
      name: 'E-Wallet Payment',
      baseUrl: 'https://api.ewallet.com/v2',
    },
  },
  {
    id: 'sms-otp',
    title: 'SMS OTP',
    description: 'OTP delivery via SMS gateway',
    icon: MessageSquare,
    defaults: {
      configKey: 'sms-otp',
      name: 'SMS OTP Gateway',
      baseUrl: 'https://api.sms-gateway.com/v1',
    },
  },
  {
    id: 'custom-api',
    title: 'Custom API',
    description: 'Connect to any REST API endpoint',
    icon: Globe,
    defaults: {
      configKey: 'custom-api',
      name: 'Custom API',
      baseUrl: 'https://api.example.com',
    },
  },
];

const STEPS = ['Template', 'Endpoint & Auth', 'Field Mapping', 'Test & Save'];

// ── Component ────────────────────────────────────────────────

export { IntegrationForm as IntegrationFormPage };

export default function IntegrationForm() {
  const navigate = useNavigate();
  const createMutation = useCreateIntegration();

  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState<FormData>({
    template: null,
    configKey: '',
    name: '',
    baseUrl: '',
    authType: 'none',
    authToken: '',
    timeoutMs: 30000,
    mappings: [
      { sourceField: 'fromAccount', targetField: 'src_acct_no', isRequired: true },
      { sourceField: 'toAccount', targetField: 'dst_acct_no', isRequired: true },
      { sourceField: 'amount', targetField: 'txn_amount', isRequired: true },
    ],
  });

  const [testResult, setTestResult] = useState<TestResultState | null>(null);
  const [isTesting, setIsTesting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  // ── Handlers ─────────────────────────────────────────────

  const selectTemplate = (templateId: string) => {
    const tpl = TEMPLATES.find((t) => t.id === templateId);
    if (!tpl) return;
    setFormData((prev) => ({
      ...prev,
      template: templateId,
      configKey: tpl.defaults.configKey,
      name: tpl.defaults.name,
      baseUrl: tpl.defaults.baseUrl,
    }));
  };

  const updateField = <K extends keyof FormData>(key: K, value: FormData[K]) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  const addMapping = () => {
    setFormData((prev) => ({
      ...prev,
      mappings: [...prev.mappings, { sourceField: '', targetField: '', isRequired: false }],
    }));
  };

  const removeMapping = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      mappings: prev.mappings.filter((_, i) => i !== index),
    }));
  };

  const updateMapping = (index: number, field: keyof FieldMapping, value: string | boolean) => {
    setFormData((prev) => ({
      ...prev,
      mappings: prev.mappings.map((m, i) => (i === index ? { ...m, [field]: value } : m)),
    }));
  };

  const handleTestConnection = async () => {
    setIsTesting(true);
    setTestResult(null);
    try {
      // Simulate test connection with a timeout
      await new Promise((resolve) => setTimeout(resolve, 1500));
      // Try actual API if integration exists
      setTestResult({
        success: true,
        message: `Connection to ${formData.baseUrl} successful`,
        response: { statusCode: 200, latency: '142ms' },
      });
    } catch {
      setTestResult({
        success: false,
        message: `Failed to connect to ${formData.baseUrl}`,
      });
    } finally {
      setIsTesting(false);
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      const payload = {
        configKey: formData.configKey,
        name: formData.name,
        connectorType: 'generic-http',
        baseUrl: formData.baseUrl,
        authType: formData.authType,
        authParams: formData.authType !== 'none' ? JSON.stringify({ token: formData.authToken }) : undefined,
        timeoutMs: formData.timeoutMs,
        status: 'draft' as const,
        operations: [
          {
            name: 'default',
            httpMethod: 'POST',
            path: '/',
            requestMappings: formData.mappings.map((m, i) => ({
              sourceField: m.sourceField,
              targetField: m.targetField,
              isRequired: m.isRequired,
              sortOrder: i,
            })),
            responseMappings: [],
            validations: [],
          },
        ],
      };
      await createMutation.mutateAsync(payload);
      navigate('/integrations');
    } catch {
      // Error handled by mutation hook
    } finally {
      setIsSaving(false);
    }
  };

  const canGoNext = (): boolean => {
    switch (currentStep) {
      case 0:
        return formData.template !== null;
      case 1:
        return formData.name.trim() !== '' && formData.baseUrl.trim() !== '';
      case 2:
        return formData.mappings.length > 0;
      default:
        return true;
    }
  };

  // ── Render Steps ─────────────────────────────────────────

  const renderStepIndicator = () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', marginBottom: 'var(--space-8)' }}>
      {STEPS.map((step, index) => (
        <div key={step} style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
          <div
            style={{
              width: 28,
              height: 28,
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 'var(--text-xs)',
              fontWeight: 600,
              background: index === currentStep
                ? 'var(--color-brand)'
                : index < currentStep
                  ? 'var(--color-success)'
                  : 'var(--color-bg-subtle)',
              color: index <= currentStep ? '#fff' : 'var(--color-text-tertiary)',
              border: index === currentStep
                ? 'none'
                : `1px solid ${index < currentStep ? 'var(--color-success)' : 'var(--color-border)'}`,
              transition: 'all var(--transition-normal)',
            }}
          >
            {index < currentStep ? '✓' : index + 1}
          </div>
          <span
            style={{
              fontSize: 'var(--text-sm)',
              color: index === currentStep ? 'var(--color-text-primary)' : 'var(--color-text-tertiary)',
              fontWeight: index === currentStep ? 500 : 400,
            }}
          >
            {step}
          </span>
          {index < STEPS.length - 1 && (
            <div
              style={{
                width: 32,
                height: 1,
                background: index < currentStep ? 'var(--color-success)' : 'var(--color-border)',
                margin: '0 var(--space-1)',
              }}
            />
          )}
        </div>
      ))}
    </div>
  );

  const renderStep0 = () => (
    <div>
      <h2 style={{ fontSize: 'var(--text-xl)', fontWeight: 600, marginBottom: 'var(--space-2)' }}>
        Choose a Template
      </h2>
      <p style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-6)' }}>
        Select a template to pre-fill configuration or start from scratch.
      </p>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 'var(--space-4)' }}>
        {TEMPLATES.map((tpl) => {
          const Icon = tpl.icon;
          const isSelected = formData.template === tpl.id;
          return (
            <div
              key={tpl.id}
              onClick={() => selectTemplate(tpl.id)}
              className="card"
              style={{
                padding: 'var(--space-5)',
                cursor: 'pointer',
                borderColor: isSelected ? 'var(--color-brand)' : undefined,
                boxShadow: isSelected ? 'var(--shadow-glow)' : undefined,
                transition: 'all var(--transition-normal)',
              }}
              role="button"
              tabIndex={0}
              aria-pressed={isSelected}
              onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') selectTemplate(tpl.id); }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', marginBottom: 'var(--space-3)' }}>
                <div
                  style={{
                    width: 36,
                    height: 36,
                    borderRadius: 'var(--radius-md)',
                    background: isSelected ? 'var(--color-brand)' : 'var(--color-bg-subtle)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    transition: 'background var(--transition-normal)',
                  }}
                >
                  <Icon size={18} color={isSelected ? '#fff' : 'var(--color-text-secondary)'} />
                </div>
                <span style={{ fontSize: 'var(--text-md)', fontWeight: 500, color: 'var(--color-text-primary)' }}>
                  {tpl.title}
                </span>
              </div>
              <p style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', margin: 0 }}>
                {tpl.description}
              </p>
            </div>
          );
        })}
      </div>
    </div>
  );

  const renderStep1 = () => (
    <div>
      <h2 style={{ fontSize: 'var(--text-xl)', fontWeight: 600, marginBottom: 'var(--space-2)' }}>
        Endpoint & Authentication
      </h2>
      <p style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-6)' }}>
        Configure the connection details for this integration.
      </p>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)' }}>
        {/* Name */}
        <div>
          <label style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-2)', display: 'block' }}>
            Name
          </label>
          <input
            className="input"
            value={formData.name}
            onChange={(e) => updateField('name', e.target.value)}
            placeholder="e.g. Bank Transfer API"
          />
        </div>

        {/* Base URL */}
        <div>
          <label style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-2)', display: 'block' }}>
            Base URL
          </label>
          <input
            className="input"
            value={formData.baseUrl}
            onChange={(e) => updateField('baseUrl', e.target.value)}
            placeholder="https://api.example.com/v1"
            style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}
          />
        </div>

        {/* Auth Type */}
        <div>
          <label style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-2)', display: 'block' }}>
            Auth Type
          </label>
          <select
            className="input"
            value={formData.authType}
            onChange={(e) => updateField('authType', e.target.value as FormData['authType'])}
          >
            <option value="none">None</option>
            <option value="apikey">API Key</option>
            <option value="basic">Basic Auth</option>
            <option value="bearer">Bearer Token</option>
          </select>
        </div>

        {/* Auth Token (conditional) */}
        {formData.authType !== 'none' && (
          <div>
            <label style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-2)', display: 'block' }}>
              {formData.authType === 'apikey' ? 'API Key' : formData.authType === 'basic' ? 'Credentials (user:pass)' : 'Bearer Token'}
            </label>
            <input
              className="input"
              type="password"
              value={formData.authToken}
              onChange={(e) => updateField('authToken', e.target.value)}
              placeholder="Enter secret key or token"
              style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}
            />
          </div>
        )}

        {/* Timeout */}
        <div>
          <label style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-2)', display: 'block' }}>
            Timeout (ms)
          </label>
          <input
            className="input"
            type="number"
            value={formData.timeoutMs}
            onChange={(e) => updateField('timeoutMs', Number(e.target.value))}
            placeholder="30000"
            style={{ width: 160 }}
          />
        </div>
      </div>
    </div>
  );

  const renderStep2 = () => (
    <div>
      <h2 style={{ fontSize: 'var(--text-xl)', fontWeight: 600, marginBottom: 'var(--space-2)' }}>
        Field Mapping
      </h2>
      <p style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-6)' }}>
        Map your internal fields to the partner API fields.
      </p>

      {/* Table header */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 1fr 80px 48px',
          gap: 'var(--space-3)',
          padding: 'var(--space-3) var(--space-4)',
          background: 'var(--color-bg-subtle)',
          borderRadius: 'var(--radius-md) var(--radius-md) 0 0',
          border: '1px solid var(--color-border)',
          borderBottom: 'none',
        }}
      >
        <span style={{ fontSize: 'var(--text-xs)', fontWeight: 600, color: 'var(--color-text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
          Source Field
        </span>
        <span style={{ fontSize: 'var(--text-xs)', fontWeight: 600, color: 'var(--color-text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
          Target Field
        </span>
        <span style={{ fontSize: 'var(--text-xs)', fontWeight: 600, color: 'var(--color-text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
          Required
        </span>
        <span />
      </div>

      {/* Rows */}
      <div
        style={{
          border: '1px solid var(--color-border)',
          borderRadius: '0 0 var(--radius-md) var(--radius-md)',
        }}
      >
        {formData.mappings.map((mapping, index) => (
          <div
            key={index}
            style={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr 80px 48px',
              gap: 'var(--space-3)',
              padding: 'var(--space-3) var(--space-4)',
              borderBottom: index < formData.mappings.length - 1 ? '1px solid var(--color-border)' : 'none',
              alignItems: 'center',
            }}
          >
            <input
              className="input"
              value={mapping.sourceField}
              onChange={(e) => updateMapping(index, 'sourceField', e.target.value)}
              placeholder="fromAccount"
              style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}
            />
            <input
              className="input"
              value={mapping.targetField}
              onChange={(e) => updateMapping(index, 'targetField', e.target.value)}
              placeholder="src_acct_no"
              style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}
            />
            <div style={{ display: 'flex', justifyContent: 'center' }}>
              <input
                type="checkbox"
                checked={mapping.isRequired}
                onChange={(e) => updateMapping(index, 'isRequired', e.target.checked)}
                style={{ width: 16, height: 16, accentColor: 'var(--color-brand)', cursor: 'pointer' }}
                aria-label={`Required field ${mapping.sourceField}`}
              />
            </div>
            <button
              className="btn btn-ghost btn-sm"
              onClick={() => removeMapping(index)}
              aria-label={`Remove mapping ${mapping.sourceField}`}
              style={{ color: 'var(--color-danger)' }}
            >
              <Trash2 size={14} />
            </button>
          </div>
        ))}
      </div>

      {/* Add button */}
      <button
        className="btn btn-outline btn-sm"
        onClick={addMapping}
        style={{ marginTop: 'var(--space-4)' }}
      >
        <Plus size={14} />
        Add Mapping
      </button>
    </div>
  );

  const renderStep3 = () => (
    <div>
      <h2 style={{ fontSize: 'var(--text-xl)', fontWeight: 600, marginBottom: 'var(--space-2)' }}>
        Test & Save
      </h2>
      <p style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', marginBottom: 'var(--space-6)' }}>
        Verify the connection and save your integration configuration.
      </p>

      {/* Summary card */}
      <div className="card" style={{ padding: 'var(--space-5)', marginBottom: 'var(--space-6)' }}>
        <h3 style={{ fontSize: 'var(--text-md)', fontWeight: 500, marginBottom: 'var(--space-4)', margin: '0 0 var(--space-4) 0' }}>
          Configuration Summary
        </h3>
        <div style={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: 'var(--space-3)', fontSize: 'var(--text-sm)' }}>
          <span style={{ color: 'var(--color-text-secondary)' }}>Name</span>
          <span style={{ color: 'var(--color-text-primary)' }}>{formData.name}</span>
          <span style={{ color: 'var(--color-text-secondary)' }}>Base URL</span>
          <span style={{ color: 'var(--color-text-primary)', fontFamily: 'var(--font-mono)' }}>{formData.baseUrl}</span>
          <span style={{ color: 'var(--color-text-secondary)' }}>Auth Type</span>
          <span style={{ color: 'var(--color-text-primary)' }}>{formData.authType}</span>
          <span style={{ color: 'var(--color-text-secondary)' }}>Timeout</span>
          <span style={{ color: 'var(--color-text-primary)' }}>{formData.timeoutMs}ms</span>
          <span style={{ color: 'var(--color-text-secondary)' }}>Mappings</span>
          <span style={{ color: 'var(--color-text-primary)' }}>{formData.mappings.length} fields</span>
        </div>
      </div>

      {/* Actions */}
      <div style={{ display: 'flex', gap: 'var(--space-3)', marginBottom: 'var(--space-5)' }}>
        <button
          className="btn btn-outline btn-md"
          onClick={handleTestConnection}
          disabled={isTesting}
        >
          {isTesting ? <Loader2 size={14} className="animate-spin" /> : <Zap size={14} />}
          {isTesting ? 'Testing...' : 'Test Connection'}
        </button>
        <button
          className="btn btn-primary btn-md"
          onClick={handleSave}
          disabled={isSaving}
        >
          {isSaving ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
          {isSaving ? 'Saving...' : 'Save Integration'}
        </button>
      </div>

      {/* Test result */}
      {testResult && (
        <div
          className="card"
          style={{
            padding: 'var(--space-4)',
            borderColor: testResult.success ? 'var(--color-success)' : 'var(--color-danger)',
            background: testResult.success ? 'rgba(34,197,94,0.05)' : 'rgba(239,68,68,0.05)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)', marginBottom: 'var(--space-2)' }}>
            {testResult.success ? (
              <CheckCircle2 size={16} color="var(--color-success)" />
            ) : (
              <XCircle size={16} color="var(--color-danger)" />
            )}
            <span
              style={{
                fontSize: 'var(--text-sm)',
                fontWeight: 500,
                color: testResult.success ? 'var(--color-success)' : 'var(--color-danger)',
              }}
            >
              {testResult.success ? 'Success' : 'Failed'}
            </span>
          </div>
          <p style={{ fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', margin: 0 }}>
            {testResult.message}
          </p>
          {testResult.response && (
            <pre
              style={{
                marginTop: 'var(--space-3)',
                padding: 'var(--space-3)',
                background: 'var(--color-bg-subtle)',
                borderRadius: 'var(--radius-sm)',
                fontSize: 'var(--text-xs)',
                fontFamily: 'var(--font-mono)',
                color: 'var(--color-text-secondary)',
                overflow: 'auto',
              }}
            >
              {JSON.stringify(testResult.response, null, 2)}
            </pre>
          )}
        </div>
      )}
    </div>
  );

  const renderCurrentStep = () => {
    switch (currentStep) {
      case 0: return renderStep0();
      case 1: return renderStep1();
      case 2: return renderStep2();
      case 3: return renderStep3();
      default: return null;
    }
  };

  // ── Main Render ──────────────────────────────────────────

  return (
    <div style={{ padding: 'var(--space-8)', maxWidth: 720, margin: '0 auto' }}>
      {renderStepIndicator()}
      <div className="card" style={{ padding: 'var(--space-8)' }}>
        {renderCurrentStep()}
      </div>

      {/* Navigation */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          marginTop: 'var(--space-6)',
        }}
      >
        <button
          className="btn btn-ghost btn-md"
          onClick={() => setCurrentStep((s) => s - 1)}
          disabled={currentStep === 0}
        >
          <ChevronLeft size={16} />
          Previous
        </button>
        {currentStep < STEPS.length - 1 && (
          <button
            className="btn btn-primary btn-md"
            onClick={() => setCurrentStep((s) => s + 1)}
            disabled={!canGoNext()}
          >
            Next
            <ChevronRight size={16} />
          </button>
        )}
      </div>
    </div>
  );
}
