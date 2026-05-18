import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Zap } from 'lucide-react';

export const IntegrationTestPage: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  return (
    <div className="flex flex-col gap-6 animate-fade-in">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/integrations')} className="btn-sm btn-outline">
          <ArrowLeft className="w-3.5 h-3.5" />
        </button>
        <div>
          <h1 className="text-3xl font-bold text-text-primary">Test Integration</h1>
          <p className="text-sm text-text-secondary mt-1">Config: {id}</p>
        </div>
      </div>

      <div className="card p-12 flex flex-col items-center gap-4 text-text-secondary">
        <div className="w-14 h-14 rounded-xl bg-brand/10 flex items-center justify-center">
          <Zap className="w-6 h-6 text-brand" />
        </div>
        <p className="text-md font-medium text-text-primary">Test panel coming soon</p>
        <button onClick={() => navigate('/integrations')} className="btn-md btn-primary mt-2">
          Back to Integrations
        </button>
      </div>
    </div>
  );
};
