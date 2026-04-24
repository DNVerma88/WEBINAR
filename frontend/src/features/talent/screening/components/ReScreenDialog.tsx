import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography } from '@/components/ui';
import { CloseIcon } from '@/components/ui';
import { Accordion, AccordionDetails, AccordionSummary, Alert, Checkbox, FormControlLabel, IconButton, ToggleButton, ToggleButtonGroup } from '@mui/material';
import { ExpandMore as ExpandMoreIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { screeningApi, DEFAULT_PROMPT_TEMPLATE } from '../../talentApi';
import { SCORING_MODES } from '../../types';
import type { ScoringMode } from '../../types';
import { useToast } from '../../../../shared/hooks/useToast';
import type { AxiosError } from 'axios';

interface Props {
  open: boolean;
  onClose: () => void;
  jobId: string;
  currentJdText?: string;
  currentPromptTemplate?: string;
}

export function ReScreenDialog({ open, onClose, jobId, currentJdText, currentPromptTemplate }: Props) {
  const [mode, setMode] = useState<ScoringMode>(SCORING_MODES.Gemini);
  const [jdText, setJdText] = useState(currentJdText ?? '');
  const [overwriteAllScores, setOverwriteAllScores] = useState(false);
  const [promptTemplate, setPromptTemplate] = useState(currentPromptTemplate ?? DEFAULT_PROMPT_TEMPLATE);
  const [apiError, setApiError] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const toast = useToast();

  // Reset JD text whenever dialog opens with new content
  const handleOpen = () => {
    setJdText(currentJdText ?? '');
    setOverwriteAllScores(false);
    setPromptTemplate(currentPromptTemplate ?? DEFAULT_PROMPT_TEMPLATE);
    setApiError(null);
  };

  const mutation = useMutation({
    mutationFn: async () => {
      // Update JD first if it has changed
      const trimmed = jdText.trim();
      if (trimmed && trimmed !== (currentJdText ?? '').trim()) {
        await screeningApi.updateJd(jobId, trimmed);
      }
      await screeningApi.reScreen(jobId, mode, overwriteAllScores, promptTemplate);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['screening', jobId] });
      void queryClient.invalidateQueries({ queryKey: ['screening', 'list'] });
      onClose();
      toast.success('Re-screening started.');
    },
    onError: (error: unknown) => {
      const detail = (error as AxiosError<{ detail?: string }>)?.response?.data?.detail;
      const msg = detail ?? 'Failed to start re-screening. Please try again.';
      setApiError(msg);
      // If all candidates are already scored, hint the user by auto-enabling the override
      if (detail?.toLowerCase().includes('already scored')) {
        setOverwriteAllScores(true);
      }
    },
  });

  const handleClose = () => {
    if (!mutation.isPending) onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth TransitionProps={{ onEnter: handleOpen }}>
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">Re-screen Resumes</Typography>
          <IconButton size="small" onClick={handleClose} disabled={mutation.isPending}>
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent>
        {/* Editable JD */}
        <Typography variant="subtitle2" fontWeight={600} mb={1}>
          Job Description
        </Typography>
        <Typography variant="body2" color="text.secondary" mb={1}>
          Update the job description before re-screening. Candidates will be scored against this text.
        </Typography>
        <TextField
          multiline
          rows={7}
          fullWidth
          value={jdText}
          onChange={(e) => setJdText(e.target.value)}
          placeholder="Paste or edit the job description here..."
          disabled={mutation.isPending}
          inputProps={{ maxLength: 100_000 }}
          helperText={`${jdText.length.toLocaleString()} / 100,000 characters`}
          sx={{ mb: 3 }}
        />

        {/* Scoring mode */}
        <Typography variant="subtitle2" fontWeight={600} mb={1}>
          Scoring Mode
        </Typography>
        <Typography variant="body2" color="text.secondary" mb={2}>
          All candidates will be re-scored using{' '}
          {mode === SCORING_MODES.Stub ? 'fast keyword-overlap (no API calls)' :
           mode === SCORING_MODES.Gemini ? 'Google Gemini AI' : 'OpenAI GPT + embeddings'}.
          Previous scores will be cleared.
        </Typography>

        <ToggleButtonGroup
          value={mode}
          exclusive
          onChange={(_, v: ScoringMode | null) => { if (v) { setMode(v); setApiError(null); } }}
          fullWidth
          color="primary"
          sx={{ mb: 1 }}
        >
          <ToggleButton value={SCORING_MODES.AI} sx={{ flexDirection: 'column', alignItems: 'flex-start', px: 2, py: 1.5 }}>
            <Typography variant="subtitle2" fontWeight={600}>OpenAI</Typography>
            <Typography variant="caption" color="text.secondary" textAlign="left">
              GPT&nbsp;+&nbsp;embeddings — deep skills analysis
            </Typography>
          </ToggleButton>
          <ToggleButton value={SCORING_MODES.Gemini} sx={{ flexDirection: 'column', alignItems: 'flex-start', px: 2, py: 1.5 }}>
            <Typography variant="subtitle2" fontWeight={600}>Gemini</Typography>
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

        {mutation.isError && apiError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {apiError}
          </Alert>
        )}

        {/* Re-score scope toggle */}
        <FormControlLabel
          sx={{ mt: 2 }}
          control={
            <Checkbox
              checked={overwriteAllScores}
              onChange={(e) => { setOverwriteAllScores(e.target.checked); setApiError(null); }}
              disabled={mutation.isPending}
              size="small"
            />
          }
          label={
            <Typography variant="body2">
              Re-score already scored candidates{' '}
              <Typography component="span" variant="body2" color="text.secondary">
                (by default only failed / unprocessed candidates are re-queued)
              </Typography>
            </Typography>
          }
        />

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
              Use <code>{'{JD_TEXT}'}</code> and <code>{'{RESUME_TEXT}'}</code> as placeholders.
            </Typography>
            <TextField
              multiline
              rows={14}
              fullWidth
              value={promptTemplate}
              onChange={(e) => { setPromptTemplate(e.target.value); setApiError(null); }}
              disabled={mutation.isPending}
              inputProps={{ style: { fontFamily: 'monospace', fontSize: 12 } }}
            />
            <Button
              size="small"
              sx={{ mt: 1 }}
              onClick={() => setPromptTemplate(DEFAULT_PROMPT_TEMPLATE)}
              disabled={mutation.isPending}
            >
              Reset to default
            </Button>
          </AccordionDetails>
        </Accordion>
      </DialogContent>

      <DialogActions>
        <Button onClick={handleClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || !jdText.trim()}
        >
          {mutation.isPending ? 'Starting…' : 'Start Re-screen'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
