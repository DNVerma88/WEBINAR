import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Tooltip,
  Typography,
} from '@mui/material';
import { ExpandMore as ExpandMoreIcon } from '@mui/icons-material';
import { useForm } from 'react-hook-form';
import { useQuery, useMutation } from '@tanstack/react-query';
import { resumeApi } from '../talentApi';
import type { ResumeProfile, ResumeProfileAdminSummary } from '../types';
import { PersonalInfoSection } from './sections/PersonalInfoSection';
import { SummarySection } from './sections/SummarySection';
import { WorkExperienceSection } from './sections/WorkExperienceSection';
import { EducationSection } from './sections/EducationSection';
import { SkillsSection } from './sections/SkillsSection';
import { CertificationsSection } from './sections/CertificationsSection';
import { ProjectsSection } from './sections/ProjectsSection';
import { AchievementsSection } from './sections/AchievementsSection';
import { usePageTitle } from '../../../shared/hooks/usePageTitle';
import { PageHeader } from '../../../shared/components/PageHeader';
import { DownloadIcon, EditIcon, CloseIcon } from '@/components/ui';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { useEffect, useState } from 'react';
import { useToast } from '../../../shared/hooks/useToast';
import { useAuth } from '../../../shared/hooks/useAuth';
import { UserRole } from '../../../shared/types';
import { ImportResumeDialog } from './components/ImportResumeDialog';

const defaultProfile: ResumeProfile = {
  id: '',
  template: 'Professional',
  personalInfo: { fullName: '', email: '', phone: '', location: '' },
  summary: '',
  workExperience: [],
  education: [],
  skills: [],
  certifications: [],
  projects: [],
  languages: [],
  publications: [],
  achievements: [],
  updatedAt: '',
};

// ── Admin edit dialog ────────────────────────────────────────────────────────
interface AdminEditResumeDialogProps {
  open: boolean;
  userId: string;
  fullName: string;
  onClose: () => void;
  onSaved: () => void;
  /** When provided (e.g. after a row-level import), seeds the form instead of the API data. */
  preFilledData?: Partial<ResumeProfile>;
}

function AdminEditResumeDialog({ open, userId, fullName, onClose, onSaved, preFilledData }: AdminEditResumeDialogProps) {
  const toast = useToast();
  const { control, handleSubmit, reset, watch, setValue } = useForm<ResumeProfile>({
    defaultValues: defaultProfile,
  });
  const template = watch('template');
  const [adminImportOpen, setAdminImportOpen] = useState(false);

  const { isLoading, data: targetProfile } = useQuery<ResumeProfile | null>({
    queryKey: ['talent', 'resume', 'admin', userId],
    queryFn: () => resumeApi.adminGetProfile(userId),
    enabled: open && !!userId,
    retry: false,
    staleTime: 0,
  });

  useEffect(() => {
    if (open) {
      // If pre-filled data was passed in (e.g. after a row-level AI import), use it;
      // otherwise fall back to whatever the API returned (or the empty default).
      reset(preFilledData ?? targetProfile ?? defaultProfile);
    }
  // preFilledData is intentionally included but only applied on open to avoid overriding user edits
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [targetProfile, open, reset]);

  const saveMutation = useMutation({
    mutationFn: (data: Partial<ResumeProfile>) => resumeApi.adminSaveProfile(userId, data),
    onSuccess: () => {
      toast.success(`Resume saved for ${fullName}.`);
      onSaved();
      onClose();
    },
    onError: () => toast.error('Failed to save resume.'),
  });

  const onSubmit = handleSubmit((data) => saveMutation.mutate(data));

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth scroll="paper">
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">Edit Resume — {fullName}</Typography>
          <IconButton size="small" onClick={onClose}>
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>
      </DialogTitle>
      <DialogContent dividers>
        {isLoading ? (
          <Box display="flex" justifyContent="center" p={4}>
            <CircularProgress />
          </Box>
        ) : (
          <Box>
            {/* Template selector */}
            <Box mb={3}>
              <FormControl size="small" sx={{ minWidth: 220 }}>
                <InputLabel>Template</InputLabel>
                <Select
                  label="Template"
                  value={template}
                  onChange={(e) =>
                    setValue('template', e.target.value as ResumeProfile['template'])
                  }
                >
                  <MenuItem value="Professional">Professional</MenuItem>
                  <MenuItem value="Modern">Modern</MenuItem>
                  <MenuItem value="Minimal">Minimal</MenuItem>
                </Select>
              </FormControl>
            </Box>
            {[
              { label: 'Personal Information', content: <PersonalInfoSection control={control} /> },
              { label: 'Professional Summary', content: <SummarySection control={control} /> },
              { label: 'Work Experience', content: <WorkExperienceSection control={control} /> },
              { label: 'Education', content: <EducationSection control={control} /> },
              { label: 'Skills', content: <SkillsSection control={control} /> },
              { label: 'Certifications', content: <CertificationsSection control={control} /> },
              { label: 'Projects', content: <ProjectsSection control={control} /> },
              { label: 'Achievements', content: <AchievementsSection control={control} /> },
            ].map(({ label, content }) => (
              <Accordion key={label} defaultExpanded={label === 'Personal Information'}>
                <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                  <Typography variant="subtitle1" fontWeight={600}>{label}</Typography>
                </AccordionSummary>
                <AccordionDetails>{content}</AccordionDetails>
              </Accordion>
            ))}
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        {/* Import button is on the left; Save/Cancel on the right */}
        <Button
          variant="outlined"
          startIcon={<UploadFileIcon />}
          onClick={() => setAdminImportOpen(true)}
          disabled={saveMutation.isPending || isLoading}
          sx={{ mr: 'auto' }}
        >
          Import from File
        </Button>
        <Button onClick={onClose} disabled={saveMutation.isPending}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => void onSubmit()}
          disabled={saveMutation.isPending || isLoading}
        >
          {saveMutation.isPending ? 'Saving…' : 'Save'}
        </Button>
      </DialogActions>

      {/* Import dialog scoped to this user's edit form */}
      <ImportResumeDialog
        open={adminImportOpen}
        onClose={() => setAdminImportOpen(false)}
        onImported={(parsed) => {
          reset((current) => ({ ...current, ...parsed }));
          toast.success(`Resume imported for ${fullName}. Review the fields and save when ready.`);
        }}
      />
    </Dialog>
  );
}

function downloadBlob(data: Blob, filename: string) {
  const url = URL.createObjectURL(data);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export default function ResumeBuilderPage() {
  usePageTitle('Resume Builder');
  const toast = useToast();
  const { hasRole } = useAuth();
  const isAdminOrAbove = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);
  const [activeTab, setActiveTab] = useState(0);
  const [editTarget, setEditTarget] = useState<{
    userId: string;
    fullName: string;
    preFilledData?: Partial<ResumeProfile>;
  } | null>(null);
  const [importOpen, setImportOpen] = useState(false);
  // Row-level import from the All Users table: opens import dialog, then edit dialog pre-filled
  const [rowImportTarget, setRowImportTarget] = useState<{ userId: string; fullName: string } | null>(null);

  const { control, handleSubmit, reset, watch, setValue } = useForm<ResumeProfile>({
    defaultValues: defaultProfile,
  });

  const template = watch('template');

  const { data: profile, isLoading } = useQuery<ResumeProfile | null>({
    queryKey: ['talent', 'resume'],
    queryFn: resumeApi.getProfile,
    retry: false,
  });

  const { data: adminSummaries, isLoading: isAdminLoading, refetch: refetchAdminSummaries } = useQuery<ResumeProfileAdminSummary[]>({
    queryKey: ['talent', 'resume', 'admin', 'all'],
    queryFn: resumeApi.adminListAll,
    enabled: isAdminOrAbove,
  });

  // A profile only exists in the DB once the user has saved at least once
  const hasProfile = !!profile;

  useEffect(() => {
    if (profile) reset(profile);
  }, [profile, reset]);

  const saveMutation = useMutation({
    mutationFn: (data: Partial<ResumeProfile>) => resumeApi.saveProfile(data),
    onSuccess: () => toast.success('Resume saved.'),
    onError: () => toast.error('Failed to save resume.'),
  });

  const onSubmit = handleSubmit((data) => saveMutation.mutate(data));

  const handleDownloadPdf = async () => {
    const blob = await resumeApi.downloadPdf();
    downloadBlob(blob, 'resume.pdf');
  };

  const handleDownloadWord = async () => {
    const blob = await resumeApi.downloadWord();
    downloadBlob(blob, 'resume.docx');
  };

  const handleAdminDownloadPdf = async (userId: string, fullName: string) => {
    try {
      const blob = await resumeApi.adminDownloadPdf(userId);
      downloadBlob(blob, `${fullName.replace(/\s+/g, '_')}_resume.pdf`);
    } catch {
      toast.error('Failed to download PDF.');
    }
  };

  const handleAdminDownloadWord = async (userId: string, fullName: string) => {
    try {
      const blob = await resumeApi.adminDownloadWord(userId);
      downloadBlob(blob, `${fullName.replace(/\s+/g, '_')}_resume.docx`);
    } catch {
      toast.error('Failed to download Word document.');
    }
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={6}>
        <CircularProgress />
      </Box>
    );
  }

  // ── Shared: personal resume form ──────────────────────────────────────────
  const personalFormActions = (
    <Stack direction="row" spacing={1}>
      <Button
        variant="outlined"
        onClick={() => setImportOpen(true)}
      >
        Import from File
      </Button>
      <Button
        variant="outlined"
        startIcon={<DownloadIcon />}
        onClick={handleDownloadPdf}
        disabled={!hasProfile}
        title={!hasProfile ? 'Save your profile first to download' : ''}
      >
        PDF
      </Button>
      <Button
        variant="outlined"
        startIcon={<DownloadIcon />}
        onClick={handleDownloadWord}
        disabled={!hasProfile}
        title={!hasProfile ? 'Save your profile first to download' : ''}
      >
        Word
      </Button>
      <Button
        variant="contained"
        onClick={onSubmit}
        disabled={saveMutation.isPending}
      >
        {saveMutation.isPending ? 'Saving…' : 'Save'}
      </Button>
    </Stack>
  );

  const personalForm = (
    <Box>
      {/* Template selector */}
      <Box mb={3}>
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel>Template</InputLabel>
          <Select
            label="Template"
            value={template}
            onChange={(e) =>
              setValue('template', e.target.value as ResumeProfile['template'])
            }
          >
            <MenuItem value="Professional">Professional</MenuItem>
            <MenuItem value="Modern">Modern</MenuItem>
            <MenuItem value="Minimal">Minimal</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {[
        { label: 'Personal Information', content: <PersonalInfoSection control={control} /> },
        { label: 'Professional Summary', content: <SummarySection control={control} /> },
        { label: 'Work Experience', content: <WorkExperienceSection control={control} /> },
        { label: 'Education', content: <EducationSection control={control} /> },
        { label: 'Skills', content: <SkillsSection control={control} /> },
        { label: 'Certifications', content: <CertificationsSection control={control} /> },
        { label: 'Projects', content: <ProjectsSection control={control} /> },
        { label: 'Achievements', content: <AchievementsSection control={control} /> },
      ].map(({ label, content }) => (
        <Accordion key={label} defaultExpanded={label === 'Personal Information'}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle1" fontWeight={600}>{label}</Typography>
          </AccordionSummary>
          <AccordionDetails>{content}</AccordionDetails>
        </Accordion>
      ))}
    </Box>
  );

  // ── Admin: all-users table ─────────────────────────────────────────────────
  const allUsersTable = (
    <Box>
      {isAdminLoading ? (
        <Box display="flex" justifyContent="center" p={3}>
          <CircularProgress size={24} />
        </Box>
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell><strong>Name</strong></TableCell>
                <TableCell><strong>Email</strong></TableCell>
                <TableCell><strong>Department</strong></TableCell>
                <TableCell><strong>Designation</strong></TableCell>
                <TableCell><strong>Status</strong></TableCell>
                <TableCell><strong>Last Updated</strong></TableCell>
                <TableCell align="right"><strong>Actions</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(adminSummaries ?? []).map((row) => (
                <TableRow key={row.userId} hover>
                  <TableCell>{row.fullName}</TableCell>
                  <TableCell>{row.email}</TableCell>
                  <TableCell>{row.department ?? '—'}</TableCell>
                  <TableCell>{row.designation ?? '—'}</TableCell>
                  <TableCell>
                    <Chip
                      label={row.hasProfile ? 'Created' : 'Not started'}
                      color={row.hasProfile ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    {row.updatedAt ? new Date(row.updatedAt).toLocaleDateString() : '—'}
                  </TableCell>
                  <TableCell align="right">
                    <Stack direction="row" spacing={1} justifyContent="flex-end">
                      <Tooltip title="Edit resume">
                        <IconButton
                          size="small"
                          onClick={() => setEditTarget({ userId: row.userId, fullName: row.fullName })}
                        >
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Import resume from file">
                        <IconButton
                          size="small"
                          onClick={() => setRowImportTarget({ userId: row.userId, fullName: row.fullName })}
                        >
                          <UploadFileIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={row.hasProfile ? 'Download PDF' : 'No resume yet'}>
                        <span>
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<DownloadIcon />}
                            disabled={!row.hasProfile}
                            onClick={() => handleAdminDownloadPdf(row.userId, row.fullName)}
                          >
                            PDF
                          </Button>
                        </span>
                      </Tooltip>
                      <Tooltip title={row.hasProfile ? 'Download Word' : 'No resume yet'}>
                        <span>
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<DownloadIcon />}
                            disabled={!row.hasProfile}
                            onClick={() => handleAdminDownloadWord(row.userId, row.fullName)}
                          >
                            Word
                          </Button>
                        </span>
                      </Tooltip>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
              {(adminSummaries ?? []).length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 3 }}>
                    No users found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <Box>
      <PageHeader
        title="Resume Builder"
        subtitle="Build and download your professional resume"
        actions={isAdminOrAbove && activeTab === 0 ? personalFormActions : (!isAdminOrAbove ? personalFormActions : undefined)}
      />

      {/* Admin: show tabs; regular users: show form directly */}
      {isAdminOrAbove ? (
        <>
          <Tabs
            value={activeTab}
            onChange={(_, v) => setActiveTab(v)}
            sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}
          >
            <Tab label="My Resume" />
            <Tab label="All Users" />
          </Tabs>

          {activeTab === 0 && personalForm}
          {activeTab === 1 && allUsersTable}
        </>
      ) : (
        personalForm
      )}

      {/* Admin edit dialog */}
      {editTarget && (
        <AdminEditResumeDialog
          open
          userId={editTarget.userId}
          fullName={editTarget.fullName}
          preFilledData={editTarget.preFilledData}
          onClose={() => setEditTarget(null)}
          onSaved={() => void refetchAdminSummaries()}
        />
      )}

      {/* Import from file dialog — for current user's own resume */}
      <ImportResumeDialog
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onImported={(parsed) => {
          reset((current) => ({ ...current, ...parsed }));
          toast.success('Resume imported. Review the fields and save when ready.');
        }}
      />

      {/* Row-level import from All Users table: parse file, then open edit dialog pre-filled */}
      {rowImportTarget && (
        <ImportResumeDialog
          open
          onClose={() => setRowImportTarget(null)}
          onImported={(parsed) => {
            const target = rowImportTarget;
            setRowImportTarget(null);
            // Open the edit dialog with parsed data pre-loaded into the form
            setEditTarget({
              userId: target.userId,
              fullName: target.fullName,
              preFilledData: parsed,
            });
          }}
          dialogTitle={`Import Resume — ${rowImportTarget.fullName}`}
        />
      )}
    </Box>
  );
}
