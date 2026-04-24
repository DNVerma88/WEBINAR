import { useState, useEffect } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Switch,
} from '@mui/material';
import { Box, Chip, Stack, Typography, IconButton } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import type {
  SurveyQuestionDto,
  SurveyQuestionType,
  AddSurveyQuestionRequest,
  UpdateSurveyQuestionRequest,
} from '../types';

const QUESTION_TYPES: { value: SurveyQuestionType; label: string }[] = [
  { value: 'Text',           label: 'Text (open-ended)' },
  { value: 'SingleChoice',   label: 'Single Choice' },
  { value: 'MultipleChoice', label: 'Multiple Choice' },
  { value: 'Rating',         label: 'Rating (numeric)' },
  { value: 'YesNo',          label: 'Yes / No' },
];

const LIKERT_OPTIONS = ['Strongly Disagree', 'Disagree', 'Neutral', 'Agree', 'Strongly Agree'];

interface QuestionEditorProps {
  open: boolean;
  question?: SurveyQuestionDto;
  onClose: () => void;
  onSave: (req: AddSurveyQuestionRequest | UpdateSurveyQuestionRequest) => void;
  loading?: boolean;
}

export function QuestionEditor({ open, question, onClose, onSave, loading = false }: QuestionEditorProps) {
  const [questionText, setQuestionText] = useState('');
  const [questionType, setQuestionType] = useState<SurveyQuestionType>('Text');
  const [options, setOptions] = useState<string[]>([]);
  const [optionInput, setOptionInput] = useState('');
  const [minRating, setMinRating] = useState(1);
  const [maxRating, setMaxRating] = useState(5);
  const [isRequired, setIsRequired] = useState(true);

  useEffect(() => {
    if (open) {
      setQuestionText(question?.questionText ?? '');
      setQuestionType(question?.questionType ?? 'Text');
      setOptions(question?.options ?? []);
      setMinRating(question?.minRating ?? 1);
      setMaxRating(question?.maxRating ?? 5);
      setIsRequired(question?.isRequired ?? true);
      setOptionInput('');
    }
  }, [open, question]);

  const needsOptions = questionType === 'SingleChoice' || questionType === 'MultipleChoice';
  const isRating = questionType === 'Rating';

  function addOption() {
    const trimmed = optionInput.trim();
    if (trimmed && !options.includes(trimmed)) {
      setOptions(prev => [...prev, trimmed]);
    }
    setOptionInput('');
  }

  function removeOption(idx: number) {
    setOptions(prev => prev.filter((_, i) => i !== idx));
  }

  function applyLikert() {
    if (options.length > 0 && !window.confirm('This will replace your current options with the Likert scale template. Continue?')) return;
    setQuestionType('SingleChoice');
    setOptions([...LIKERT_OPTIONS]);
  }

  function applyNps() {
    if ((questionType === 'Rating' && (minRating !== 1 || maxRating !== 5)) &&
        !window.confirm('This will change the rating range to 0–10. Continue?')) return;
    setQuestionType('Rating');
    setMinRating(0);
    setMaxRating(10);
  }

  function handleSave() {
    const req: AddSurveyQuestionRequest = {
      questionText,
      questionType,
      isRequired,
      ...(needsOptions ? { options } : {}),
      ...(isRating ? { minRating, maxRating } : {}),
    };
    onSave(question ? { ...req, recordVersion: question.orderSequence } : req);
  }

  const canSave =
    questionText.trim().length > 0 &&
    (!needsOptions || options.length >= 2);

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{question ? 'Edit Question' : 'Add Question'}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Question Text"
            fullWidth
            multiline
            rows={2}
            value={questionText}
            onChange={e => setQuestionText(e.target.value)}
          />

          <FormControl fullWidth>
            <InputLabel>Question Type</InputLabel>
            <Select
              value={questionType}
              label="Question Type"
              onChange={e => setQuestionType(e.target.value as SurveyQuestionType)}
            >
              {QUESTION_TYPES.map(t => (
                <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" onClick={applyLikert}>
              5-Point Likert Template
            </Button>
            <Button size="small" variant="outlined" onClick={applyNps}>
              NPS (0–10) Template
            </Button>
          </Stack>

          {needsOptions && (
            <Box>
              <Typography variant="body2" sx={{ mb: 1 }}>Options (min 2)</Typography>
              <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
                <TextField
                  size="small"
                  placeholder="Add option"
                  value={optionInput}
                  onChange={e => setOptionInput(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && addOption()}
                  fullWidth
                />
                <IconButton onClick={addOption} size="small"><AddIcon /></IconButton>
              </Stack>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {options.map((opt, i) => (
                  <Chip
                    key={i}
                    label={opt}
                    onDelete={() => removeOption(i)}
                    deleteIcon={<DeleteIcon />}
                  />
                ))}
              </Box>
            </Box>
          )}

          {isRating && (
            <Stack direction="row" spacing={2}>
              <TextField
                label="Min Rating"
                type="number"
                size="small"
                value={minRating}
                onChange={e => setMinRating(Number(e.target.value))}
                inputProps={{ min: 0, max: 9 }}
              />
              <TextField
                label="Max Rating"
                type="number"
                size="small"
                value={maxRating}
                onChange={e => setMaxRating(Number(e.target.value))}
                inputProps={{ min: 1, max: 10 }}
              />
            </Stack>
          )}

          <FormControlLabel
            control={
              <Switch
                checked={isRequired}
                onChange={e => setIsRequired(e.target.checked)}
              />
            }
            label="Required"
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={!canSave || loading}
        >
          {loading ? 'Saving…' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
