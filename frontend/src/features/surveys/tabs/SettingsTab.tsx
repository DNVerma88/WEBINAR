import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  FormControlLabel,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { surveysApi } from '../api/surveysApi';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import type { SurveyDto, UpdateSurveyRequest } from '../types';

interface Props {
  survey: SurveyDto;
}

export function SettingsTab({ survey }: Props) {
  const qc = useQueryClient();
  const isDraft = survey.status === 'Draft';

  const toDateInputValue = (iso?: string) => {
    if (!iso) return '';
    // ISO string: "2025-08-01T00:00:00Z" → "2025-08-01"
    return iso.split('T')[0];
  };

  const [title, setTitle] = useState(survey.title);
  const [description, setDescription] = useState(survey.description ?? '');
  const [welcomeMessage, setWelcomeMessage] = useState(survey.welcomeMessage ?? '');
  const [thankYouMessage, setThankYouMessage] = useState(survey.thankYouMessage ?? '');
  const [endsAt, setEndsAt] = useState(toDateInputValue(survey.endsAt));
  const [isAnonymous, setIsAnonymous] = useState(survey.isAnonymous);
  const [saved, setSaved] = useState(false);

  const updateMut = useMutation({
    mutationFn: (req: UpdateSurveyRequest) => surveysApi.updateSurvey(survey.id, req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['surveys', survey.id] });
      qc.invalidateQueries({ queryKey: ['surveys', 'list'] });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    },
  });

  function handleSave() {
    updateMut.mutate({
      title: title.trim(),
      description: description.trim() || undefined,
      welcomeMessage: welcomeMessage.trim() || undefined,
      thankYouMessage: thankYouMessage.trim() || undefined,
      endsAt: endsAt ? new Date(endsAt).toISOString() : undefined,
      isAnonymous,
      recordVersion: survey.recordVersion,
    });
  }

  const isDirty =
    title !== survey.title ||
    description !== (survey.description ?? '') ||
    welcomeMessage !== (survey.welcomeMessage ?? '') ||
    thankYouMessage !== (survey.thankYouMessage ?? '') ||
    endsAt !== toDateInputValue(survey.endsAt) ||
    isAnonymous !== survey.isAnonymous;

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Survey Settings
        </Typography>

        {!isDraft && (
          <Alert severity="info" sx={{ mb: 2 }}>
            This survey has been launched. Settings are read-only.
          </Alert>
        )}

        <Stack spacing={2}>
          <TextField
            label="Title"
            required
            fullWidth
            value={title}
            onChange={e => setTitle(e.target.value)}
            disabled={!isDraft}
          />
          <TextField
            label="Description"
            fullWidth
            multiline
            rows={2}
            value={description}
            onChange={e => setDescription(e.target.value)}
            disabled={!isDraft}
          />
          <TextField
            label="Survey End Date"
            type="date"
            fullWidth
            value={endsAt}
            onChange={e => setEndsAt(e.target.value)}
            disabled={!isDraft}
            slotProps={{
              inputLabel: { shrink: true },
              htmlInput: { min: new Date().toISOString().split('T')[0] },
            }}
            helperText="Invitation links will expire on this date. Leave blank for no fixed end date."
          />
          <TextField
            label="Welcome Message"
            fullWidth
            multiline
            rows={2}
            value={welcomeMessage}
            onChange={e => setWelcomeMessage(e.target.value)}
            disabled={!isDraft}
            helperText="Shown to respondents before they start the survey."
          />
          <TextField
            label="Thank You Message"
            fullWidth
            multiline
            rows={2}
            value={thankYouMessage}
            onChange={e => setThankYouMessage(e.target.value)}
            disabled={!isDraft}
            helperText="Shown after a respondent submits their answers."
          />
          <FormControlLabel
            control={
              <Switch
                checked={isAnonymous}
                onChange={e => setIsAnonymous(e.target.checked)}
                disabled={!isDraft}
              />
            }
            label="Anonymous responses"
          />

          {updateMut.isError && <ApiErrorAlert error={updateMut.error} />}
          {saved && <Alert severity="success">Settings saved successfully.</Alert>}

          {isDraft && (
            <Box>
              <Button
                variant="contained"
                startIcon={updateMut.isPending ? <CircularProgress size={16} /> : <SaveIcon />}
                onClick={handleSave}
                disabled={!title.trim() || !isDirty || updateMut.isPending}
              >
                {updateMut.isPending ? 'Saving…' : 'Save Settings'}
              </Button>
            </Box>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
}
