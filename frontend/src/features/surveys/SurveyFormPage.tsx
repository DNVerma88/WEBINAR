import { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Container,
  Divider,
  FormControl,
  FormControlLabel,
  FormGroup,
  FormLabel,
  Paper,
  Radio,
  RadioGroup,
  Slider,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { useParams } from 'react-router-dom';
import { surveyFormApi } from './api/surveyFormApi';
import type { SurveyFormDto, SurveyQuestionDto, SurveyAnswerRequest } from './types';

type FormState = 'loading' | 'ready' | 'submitting' | 'submitted' | 'expired' | 'already_submitted' | 'closed' | 'not_found' | 'error';

export default function SurveyFormPage() {
  const { token } = useParams<{ token: string }>();
  const [formState, setFormState] = useState<FormState>('loading');
  const [survey, setSurvey] = useState<SurveyFormDto | null>(null);
  const [answers, setAnswers] = useState<Map<string, SurveyAnswerRequest>>(new Map());
  const [validationErrors, setValidationErrors] = useState<Set<string>>(new Set());
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    if (!token) { setFormState('not_found'); return; }

    surveyFormApi.getForm(token)
      .then(data => {
        setSurvey(data);
        setFormState('ready');
      })
      .catch((err: unknown) => {
        const status = (err as { response?: { status?: number; data?: { detail?: string } } })?.response?.status;
        const detail = (err as { response?: { status?: number; data?: { detail?: string } } })?.response?.data?.detail ?? '';

        if (status === 400) {
          const lower = detail.toLowerCase();
          if (lower.includes('expired')) setFormState('expired');
          else if (lower.includes('already') || lower.includes('submitted')) setFormState('already_submitted');
          else if (lower.includes('closed')) setFormState('closed');
          else { setFormState('error'); setErrorMessage(detail || 'Invalid survey link.'); }
        } else if (status === 404) {
          setFormState('not_found');
        } else {
          setFormState('error');
          setErrorMessage('Failed to load survey. Please try again later.');
        }
      });
  }, [token]);

  function setAnswer(q: SurveyQuestionDto, partial: Partial<SurveyAnswerRequest>) {
    setAnswers(prev => {
      const next = new Map(prev);
      next.set(q.id, { questionId: q.id, ...prev.get(q.id), ...partial });
      return next;
    });
    setValidationErrors(prev => { const s = new Set(prev); s.delete(q.id); return s; });
  }

  function validate(): boolean {
    if (!survey) return false;
    const errors = new Set<string>();
    for (const q of survey.questions) {
      if (!q.isRequired) continue;
      const ans = answers.get(q.id);
      if (!ans) { errors.add(q.id); continue; }
      if (q.questionType === 'Text' && !ans.answerText?.trim()) errors.add(q.id);
      if ((q.questionType === 'SingleChoice' || q.questionType === 'YesNo') && !ans.answerText) errors.add(q.id);
      if (q.questionType === 'MultipleChoice' && (!ans.answerOptions || ans.answerOptions.length === 0)) errors.add(q.id);
      if (q.questionType === 'Rating' && ans.ratingValue === undefined) errors.add(q.id);
    }
    setValidationErrors(errors);
    return errors.size === 0;
  }

  async function handleSubmit() {
    if (!validate() || !token) return;
    setFormState('submitting');
    try {
      await surveyFormApi.submit(token, {
        answers: survey!.questions.map(q => answers.get(q.id) ?? { questionId: q.id }),
      });
      setFormState('submitted');
    } catch {
      setFormState('error');
      setErrorMessage('Submission failed. Please try again.');
    }
  }

  // ── Render states ──────────────────────────────────────────────────────────

  if (formState === 'loading') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (formState === 'submitted') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', bgcolor: 'background.default' }}>
        <Paper sx={{ p: 5, maxWidth: 500, textAlign: 'center' }}>
          <CheckCircleIcon color="success" sx={{ fontSize: 64, mb: 2 }} />
          <Typography variant="h5" gutterBottom>Thank You!</Typography>
          <Typography color="text.secondary">
            {survey?.thankYouMessage ?? 'Your response has been recorded.'}
          </Typography>
        </Paper>
      </Box>
    );
  }

  if (formState === 'expired') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', bgcolor: 'background.default' }}>
        <Paper sx={{ p: 5, maxWidth: 500, textAlign: 'center' }}>
          <Typography variant="h5" gutterBottom color="warning.main">Link Expired</Typography>
          <Typography color="text.secondary">
            This survey link has expired. Please contact your administrator to request a new link.
          </Typography>
        </Paper>
      </Box>
    );
  }

  if (formState === 'already_submitted') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', bgcolor: 'background.default' }}>
        <Paper sx={{ p: 5, maxWidth: 500, textAlign: 'center' }}>
          <CheckCircleIcon color="success" sx={{ fontSize: 64, mb: 2 }} />
          <Typography variant="h5" gutterBottom>Already Submitted</Typography>
          <Typography color="text.secondary">You have already responded to this survey. Thank you!</Typography>
        </Paper>
      </Box>
    );
  }

  if (formState === 'closed') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', bgcolor: 'background.default' }}>
        <Paper sx={{ p: 5, maxWidth: 500, textAlign: 'center' }}>
          <Typography variant="h5" gutterBottom color="error">Survey Closed</Typography>
          <Typography color="text.secondary">This survey is no longer accepting responses.</Typography>
        </Paper>
      </Box>
    );
  }

  if (formState === 'not_found' || formState === 'error') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', bgcolor: 'background.default' }}>
        <Paper sx={{ p: 5, maxWidth: 500, textAlign: 'center' }}>
          <Typography variant="h5" gutterBottom color="error">
            {formState === 'not_found' ? 'Survey Not Found' : 'Error'}
          </Typography>
          <Typography color="text.secondary">
            {formState === 'not_found'
              ? 'This link is invalid or no longer exists.'
              : errorMessage}
          </Typography>
        </Paper>
      </Box>
    );
  }

  if (!survey) return null;

  // ── Main form ──────────────────────────────────────────────────────────────

  return (
    <Box sx={{ bgcolor: 'background.default', minHeight: '100vh', py: 5 }}>
      <Container maxWidth="md">
        {/* Welcome card */}
        <Paper sx={{ p: 4, mb: 3 }}>
          <Typography variant="h4" gutterBottom>{survey.title}</Typography>
          {survey.welcomeMessage && (
            <Typography color="text.secondary">{survey.welcomeMessage}</Typography>
          )}
          {survey.description && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>{survey.description}</Typography>
          )}
        </Paper>

        {/* Questions */}
        <Stack spacing={3}>
          {survey.questions.map((q, idx) => {
            const ans = answers.get(q.id);
            const hasError = validationErrors.has(q.id);

            return (
              <Paper key={q.id} sx={{ p: 3, border: hasError ? '1px solid' : undefined, borderColor: hasError ? 'error.main' : undefined }}>
                <FormControl fullWidth error={hasError}>
                  <FormLabel sx={{ mb: 1 }}>
                    <Typography variant="subtitle1" component="span">
                      {idx + 1}. {q.questionText}
                      {q.isRequired && <Typography component="span" color="error"> *</Typography>}
                    </Typography>
                  </FormLabel>

                  {q.questionType === 'Text' && (
                    <TextField
                      multiline
                      rows={3}
                      fullWidth
                      placeholder="Type your answer here…"
                      value={ans?.answerText ?? ''}
                      onChange={e => setAnswer(q, { answerText: e.target.value })}
                      error={hasError}
                    />
                  )}

                  {q.questionType === 'SingleChoice' && (
                    <RadioGroup
                      value={ans?.answerText ?? ''}
                      onChange={e => setAnswer(q, { answerText: e.target.value })}
                    >
                      {(q.options ?? []).map(opt => (
                        <FormControlLabel key={opt} value={opt} control={<Radio />} label={opt} />
                      ))}
                    </RadioGroup>
                  )}

                  {q.questionType === 'MultipleChoice' && (
                    <FormGroup>
                      {(q.options ?? []).map(opt => (
                        <FormControlLabel
                          key={opt}
                          control={
                            <Checkbox
                              checked={(ans?.answerOptions ?? []).includes(opt)}
                              onChange={e => {
                                const current = ans?.answerOptions ?? [];
                                const updated = e.target.checked
                                  ? [...current, opt]
                                  : current.filter(o => o !== opt);
                                setAnswer(q, { answerOptions: updated });
                              }}
                            />
                          }
                          label={opt}
                        />
                      ))}
                    </FormGroup>
                  )}

                  {q.questionType === 'Rating' && (
                    <Box sx={{ px: 2, pt: 2 }}>
                      <Slider
                        min={q.minRating}
                        max={q.maxRating}
                        step={1}
                        marks
                        valueLabelDisplay="auto"
                        value={ans?.ratingValue ?? q.minRating}
                        onChange={(_e, val) => setAnswer(q, { ratingValue: val as number })}
                      />
                      <Stack direction="row" justifyContent="space-between">
                        <Typography variant="caption">{q.minRating}</Typography>
                        <Typography variant="caption">{q.maxRating}</Typography>
                      </Stack>
                    </Box>
                  )}

                  {q.questionType === 'YesNo' && (
                    <RadioGroup
                      row
                      value={ans?.answerText ?? ''}
                      onChange={e => setAnswer(q, { answerText: e.target.value })}
                    >
                      <FormControlLabel value="Yes" control={<Radio />} label="Yes" />
                      <FormControlLabel value="No" control={<Radio />} label="No" />
                    </RadioGroup>
                  )}

                  {hasError && (
                    <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                      This field is required.
                    </Typography>
                  )}
                </FormControl>
              </Paper>
            );
          })}
        </Stack>

        <Divider sx={{ my: 4 }} />

        {errorMessage && (
          <Typography color="error" sx={{ mb: 2 }}>{errorMessage}</Typography>
        )}

        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
          <Button
            variant="contained"
            size="large"
            onClick={handleSubmit}
            disabled={formState === 'submitting'}
            startIcon={formState === 'submitting' ? <CircularProgress size={20} color="inherit" /> : undefined}
          >
            {formState === 'submitting' ? 'Submitting…' : 'Submit Survey'}
          </Button>
        </Box>
      </Container>
    </Box>
  );
}
