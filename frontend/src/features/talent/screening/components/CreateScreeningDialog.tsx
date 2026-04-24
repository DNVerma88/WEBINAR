import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Step,
  StepLabel,
  Stepper,
  TextField,
  Typography,
} from '@/components/ui';
import { CloseIcon } from '@/components/ui';
import { Accordion, AccordionDetails, AccordionSummary, Alert, Chip, IconButton, ToggleButton, ToggleButtonGroup } from '@mui/material';
import { ExpandMore as ExpandMoreIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { screeningApi, DEFAULT_PROMPT_TEMPLATE } from '../../talentApi';
import { JdInputPanel } from './JdInputPanel';
import { ResumeSourcePanel } from './ResumeSourcePanel';
import type { StorageFileRef, ScoringMode } from '../../types';
import { SCORING_MODES } from '../../types';
import { useToast } from '../../../../shared/hooks/useToast';

const STEPS = ['Job Details', 'Add Resumes', 'Review & Start'];

interface Props {
  open: boolean;
  onClose: () => void;
  onJobCreated: (jobId: string) => void;
}

export function CreateScreeningDialog({ open, onClose, onJobCreated }: Props) {
  const toast = useToast();
  const [step, setStep] = useState(0);
  const [jobTitle, setJobTitle] = useState('');
  const [jdText, setJdText] = useState<string | undefined>();
  const [jdFileRef, setJdFileRef] = useState<StorageFileRef | undefined>();
  const [createdJobId, setCreatedJobId] = useState<string | null>(null);
  const [candidateCount, setCandidateCount] = useState(0);
  const [scoringMode, setScoringMode] = useState<ScoringMode>(SCORING_MODES.Gemini);
  const [promptTemplate, setPromptTemplate] = useState(DEFAULT_PROMPT_TEMPLATE);

  const { data: aiStatus } = useQuery({
    queryKey: ['ai-status'],
    queryFn: () => screeningApi.getAiStatus(),
    staleTime: 5 * 60 * 1000,
  });

  const createJobMutation = useMutation({
    mutationFn: () =>
      screeningApi.createJob({
        jobTitle,
        jdText,
        jdFileReference: jdFileRef,
      }),
    onSuccess: (job) => {
      setCreatedJobId(job.id);
      setStep(1);
    },
    onError: () => toast.error('Failed to create screening job.'),
  });

  const startMutation = useMutation({
    mutationFn: () => screeningApi.startScreening(createdJobId!, scoringMode, promptTemplate),
    onSuccess: () => {
      onJobCreated(createdJobId!);
      handleClose();
      toast.success('Screening started.');
    },
    onError: () => toast.error('Failed to start screening.'),
  });

  const handleClose = () => {
    setStep(0);
    setJobTitle('');
    setJdText(undefined);
    setJdFileRef(undefined);
    setCreatedJobId(null);
    setCandidateCount(0);
    setPromptTemplate(DEFAULT_PROMPT_TEMPLATE);
    onClose();
  };

  const handleJdReady = (text?: string, fileRef?: StorageFileRef) => {
    setJdText(text);
    setJdFileRef(fileRef);
  };

  const handleNext = () => {
    if (step === 0) {
      if (!jobTitle.trim()) return;
      createJobMutation.mutate();
    } else if (step === 1) {
      setStep(2);
    }
  };

  const canProceedStep0 = jobTitle.trim().length > 0;

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Typography variant="h6">New Screening Job</Typography>
          <IconButton onClick={handleClose} size="small">
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent dividers>
        <Stepper activeStep={step} sx={{ mb: 3 }}>
          {STEPS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {step === 0 && (
          <Box>
            <TextField
              label="Job Title"
              fullWidth
              value={jobTitle}
              onChange={(e) => setJobTitle(e.target.value)}
              sx={{ mb: 3 }}
              placeholder="e.g. Senior Software Engineer"
              autoFocus
            />
            <Typography variant="subtitle2" gutterBottom>
              Job Description (optional)
            </Typography>
            <JdInputPanel onJdReady={handleJdReady} />
          </Box>
        )}

        {step === 1 && createdJobId && (
          <ResumeSourcePanel
            jobId={createdJobId}
            onFilesAdded={() => setCandidateCount((c) => c + 1)}
          />
        )}

        {step === 2 && (
          <Box>
            <Typography variant="subtitle1" gutterBottom>
              Review
            </Typography>
            <Box component="dl" sx={{ '& dt': { fontWeight: 600 }, '& dd': { ml: 2, mb: 1 } }}>
              <dt>Job Title</dt>
              <dd>{jobTitle}</dd>
              <dt>Candidates Added</dt>
              <dd>{candidateCount}</dd>
              {jdText && (
                <>
                  <dt>JD Preview</dt>
                  <dd>
                    <Typography variant="body2" color="text.secondary" noWrap>
                      {jdText.slice(0, 120)}…
                    </Typography>
                  </dd>
                </>
              )}
            </Box>

            {/* Scoring mode selector */}
            <Typography variant="subtitle2" fontWeight={600} mt={2} mb={1}>
              Scoring Mode
            </Typography>
            <ToggleButtonGroup
              value={scoringMode}
              exclusive
              onChange={(_, v: ScoringMode | null) => { if (v) setScoringMode(v); }}
              fullWidth
              color="primary"
              sx={{ mb: 1.5 }}
            >
              <ToggleButton value={SCORING_MODES.AI} sx={{ flexDirection: 'column', alignItems: 'flex-start', px: 2, py: 1.5 }}>
                <Box display="flex" alignItems="center" gap={0.5}>
                  <Typography variant="subtitle2" fontWeight={600}>OpenAI</Typography>
                  {aiStatus && (
                    <Chip
                      label={aiStatus.openAiConfigured ? aiStatus.openAiModel : 'Not configured'}
                      size="small"
                      color={aiStatus.openAiConfigured ? 'success' : 'error'}
                      variant="outlined"
                      sx={{ fontSize: '0.65rem', height: 18 }}
                    />
                  )}
                </Box>
                <Typography variant="caption" color="text.secondary" textAlign="left">
                  GPT&nbsp;+&nbsp;embeddings — deep skills analysis
                </Typography>
              </ToggleButton>
              <ToggleButton value={SCORING_MODES.Gemini} sx={{ flexDirection: 'column', alignItems: 'flex-start', px: 2, py: 1.5 }}>
                <Box display="flex" alignItems="center" gap={0.5}>
                  <Typography variant="subtitle2" fontWeight={600}>Gemini</Typography>
                  {aiStatus && (
                    <Chip
                      label={aiStatus.geminiConfigured ? aiStatus.geminiModel : 'Not configured'}
                      size="small"
                      color={aiStatus.geminiConfigured ? 'success' : 'error'}
                      variant="outlined"
                      sx={{ fontSize: '0.65rem', height: 18 }}
                    />
                  )}
                </Box>
                <Typography variant="caption" color="text.secondary" textAlign="left">
                  Google Gemini AI — large context, fast scoring
                </Typography>
              </ToggleButton>
              <ToggleButton value={SCORING_MODES.Stub} sx={{ flexDirection: 'column', alignItems: 'flex-start', px: 2, py: 1.5 }}>
                <Typography variant="subtitle2" fontWeight={600}>Stub Mode</Typography>
                <Typography variant="caption" color="text.secondary" textAlign="left">
                  Fast keyword-overlap, no API calls
                </Typography>
              </ToggleButton>
            </ToggleButtonGroup>

            {/* Warning when selected provider is not configured */}
            {scoringMode === SCORING_MODES.AI && aiStatus && !aiStatus.openAiConfigured && (
              <Alert severity="warning" sx={{ mb: 1 }}>
                OpenAI API key is not configured — screening will fall back to Stub mode.
              </Alert>
            )}
            {scoringMode === SCORING_MODES.Gemini && aiStatus && !aiStatus.geminiConfigured && (
              <Alert severity="warning" sx={{ mb: 1 }}>
                Gemini API key is not configured — screening will fall back to Stub mode.
              </Alert>
            )}

            {/* Prompt Template */}
            <Accordion variant="outlined" sx={{ mt: 2 }} disableGutters>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="subtitle2" fontWeight={600}>AI Prompt Template</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ ml: 1, alignSelf: 'center' }}>
                  (optional — tweak how the AI evaluates candidates)
                </Typography>
              </AccordionSummary>
              <AccordionDetails sx={{ pt: 0 }}>
                <Typography variant="body2" color="text.secondary" mb={1}>
                  This prompt is sent to the AI for each candidate. Use <code>{'{JD_TEXT}'}</code> and{' '}
                  <code>{'{RESUME_TEXT}'}</code> as placeholders.
                </Typography>
                <TextField
                  multiline
                  rows={14}
                  fullWidth
                  value={promptTemplate}
                  onChange={(e) => setPromptTemplate(e.target.value)}
                  inputProps={{ style: { fontFamily: 'monospace', fontSize: 12 } }}
                />
                <Button
                  size="small"
                  sx={{ mt: 1 }}
                  onClick={() => setPromptTemplate(DEFAULT_PROMPT_TEMPLATE)}
                >
                  Reset to default
                </Button>
              </AccordionDetails>
            </Accordion>

            <Typography variant="body2" color="text.secondary" mt={1}>
              Click "Start Screening" to begin analysis. You can close this dialog and monitor progress from the job detail page.
            </Typography>
          </Box>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        {step > 0 && step < 2 && (
          <Button onClick={() => setStep((s) => s - 1)} disabled={createJobMutation.isPending}>
            Back
          </Button>
        )}
        <Box flex={1} />
        {step < 2 && (
          <Button
            variant="contained"
            onClick={handleNext}
            disabled={
              (step === 0 && (!canProceedStep0 || createJobMutation.isPending)) ||
              (step === 1 && candidateCount === 0)
            }
          >
            {step === 0 && createJobMutation.isPending ? 'Creating…' : 'Next'}
          </Button>
        )}
        {step === 2 && (
          <Button
            variant="contained"
            color="primary"
            onClick={() => startMutation.mutate()}
            disabled={startMutation.isPending || candidateCount === 0}
          >
            {startMutation.isPending ? 'Starting…' : 'Start Screening'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
