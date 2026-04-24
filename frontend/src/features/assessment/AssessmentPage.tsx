import {
  Box,
  Card,
  Tab,
  Tabs,
} from '@/components/ui';
import { lazy, Suspense, useState } from 'react';
import { PageHeader } from '../../shared/components/PageHeader';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { GroupsTab } from './tabs/GroupsTab';
import { PeriodsTab } from './tabs/PeriodsTab';
import { AssessmentGridTab } from './tabs/AssessmentGridTab';
import { RatingScalesTab } from './tabs/RatingScalesTab';
import { RubricsTab } from './tabs/RubricsTab';
import { ParametersTab } from './tabs/ParametersTab';
import { DashboardTab } from './tabs/DashboardTab';
import { AuditTab } from './tabs/AuditTab';
import { WorkRolesTab } from './tabs/WorkRolesTab';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';

// FE-16: lazy-load ReportsTab so recharts (~380 KB) is only parsed and executed
// when the user actually visits the Reports tab, not on Assessment page first load
const ReportsTab = lazy(() =>
  import('./tabs/ReportsTab').then((m) => ({ default: m.ReportsTab }))
);

const TABS = [
  { label: 'Dashboard',      value: 'dashboard' },
  { label: 'Assessment Grid', value: 'grid' },
  { label: 'Groups',         value: 'groups' },
  { label: 'Periods',        value: 'periods' },
  { label: 'Rating Scales',  value: 'scales' },
  { label: 'Rubrics',        value: 'rubrics' },
  { label: 'Parameters',     value: 'parameters' },
  { label: 'Work Roles',     value: 'work-roles' },
  { label: 'Reports',        value: 'reports' },
  { label: 'Audit Log',      value: 'audit' },
];

export default function AssessmentPage() {
  usePageTitle('Assessment');
  const [tab, setTab] = useState('dashboard');

  return (
    <Box>
      <PageHeader
        title="Assessment"
        subtitle="Track and manage assessments across your organisation"
      />

      <Card sx={{ mb: 3 }}>
        <Tabs
          value={tab}
          onChange={(_, v) => setTab(v)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          {TABS.map((t) => (
            <Tab key={t.value} label={t.label} value={t.value} />
          ))}
        </Tabs>
      </Card>

      <Box role="tabpanel">
        {tab === 'dashboard'   && <DashboardTab />}
        {tab === 'grid'        && <AssessmentGridTab />}
        {tab === 'groups'      && <GroupsTab />}
        {tab === 'periods'     && <PeriodsTab />}
        {tab === 'scales'      && <RatingScalesTab />}
        {tab === 'rubrics'     && <RubricsTab />}
        {tab === 'parameters'  && <ParametersTab />}
        {tab === 'work-roles'  && <WorkRolesTab />}
        {tab === 'reports'     && <Suspense fallback={<LoadingOverlay />}><ReportsTab /></Suspense>}
        {tab === 'audit'       && <AuditTab />}
      </Box>
    </Box>
  );
}
