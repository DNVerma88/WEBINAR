import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Button, Card, CircularProgress, Tab, Tabs, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useQuery } from '@tanstack/react-query';
import { PageHeader } from '../../shared/components/PageHeader';
import { surveysApi } from './api/surveysApi';
import { SurveyStatusChip } from './components/SurveyStatusChip';
import { QuestionsTab } from './tabs/QuestionsTab';
import { ResultsTab } from './tabs/ResultsTab';
import { InvitationsTab } from './tabs/InvitationsTab';
import { AnalyticsTab } from './tabs/AnalyticsTab';
import { SettingsTab } from './tabs/SettingsTab';

export default function SurveyBuilderPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [tab, setTab] = useState('questions');

  const { data: survey, isLoading, error } = useQuery({
    queryKey: ['surveys', id],
    queryFn: () => surveysApi.getSurveyById(id!),
    enabled: !!id,
  });

  if (isLoading || !survey) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
        {isLoading ? <CircularProgress /> : <Typography color="error">{String(error)}</Typography>}
      </Box>
    );
  }

  const showResultsTabs = survey.status !== 'Draft';

  return (
    <Box>
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/admin/surveys')}
        sx={{ mb: 1 }}
      >
        Back to Surveys
      </Button>
      <PageHeader
        title={survey.title}
        subtitle={survey.description ?? ''}
        actions={<SurveyStatusChip status={survey.status} />}
      />

      <Card sx={{ mb: 3 }}>
        <Tabs
          value={tab}
          onChange={(_, v: string) => setTab(v)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Questions" value="questions" />
          <Tab label="Settings" value="settings" />
          {showResultsTabs && <Tab label="Results" value="results" />}
          {showResultsTabs && <Tab label="Invitations" value="invitations" />}
          {showResultsTabs && <Tab label="Analytics" value="analytics" />}
        </Tabs>
      </Card>

      <Box role="tabpanel">
        {tab === 'questions'   && <QuestionsTab survey={survey} />}
        {tab === 'settings'    && <SettingsTab survey={survey} />}
        {tab === 'results'     && showResultsTabs && <ResultsTab surveyId={survey.id} />}
        {tab === 'invitations' && showResultsTabs && <InvitationsTab surveyId={survey.id} />}
        {tab === 'analytics'   && showResultsTabs && <AnalyticsTab surveyId={survey.id} />}
      </Box>
    </Box>
  );
}
