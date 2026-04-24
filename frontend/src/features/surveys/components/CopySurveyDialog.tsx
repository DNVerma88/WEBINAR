import { useState, useEffect } from 'react';
import {
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  List,
  ListItem,
  TextField,
  Typography,
} from '@mui/material';
import type { SurveyDto, SurveyQuestionDto } from '../types';

interface CopySurveyDialogProps {
  open: boolean;
  survey: SurveyDto | null;
  questions: SurveyQuestionDto[];
  onClose: () => void;
  onCopy: (newTitle: string, excludeIds: string[]) => void;
  loading?: boolean;
}

export function CopySurveyDialog({
  open,
  survey,
  questions,
  onClose,
  onCopy,
  loading = false,
}: CopySurveyDialogProps) {
  const [newTitle, setNewTitle] = useState('');
  const [excludedIds, setExcludedIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (open) {
      setNewTitle('');
      setExcludedIds(new Set());
    }
  }, [open]);

  function toggleExclude(id: string) {
    setExcludedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function handleCopy() {
    const title = newTitle.trim() || `Copy of ${survey?.title ?? ''}`;
    onCopy(title, [...excludedIds]);
  }

  return (
    <Dialog open={open} onClose={loading ? undefined : onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Copy Survey</DialogTitle>
      <DialogContent>
        <TextField
          label="New Survey Title"
          fullWidth
          placeholder={`Copy of ${survey?.title ?? ''}`}
          value={newTitle}
          onChange={e => setNewTitle(e.target.value)}
          sx={{ mt: 1, mb: 2 }}
        />

        <Typography variant="body2" sx={{ mb: 1 }}>
          Uncheck questions to exclude from the copy:
        </Typography>
        <List dense sx={{ maxHeight: 240, overflow: 'auto' }}>
          {questions.map(q => (
            <ListItem key={q.id} disableGutters>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={!excludedIds.has(q.id)}
                    onChange={() => toggleExclude(q.id)}
                  />
                }
                label={`Q${q.orderSequence + 1}: ${q.questionText}`}
                sx={{ width: '100%' }}
              />
            </ListItem>
          ))}
        </List>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleCopy}
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} /> : undefined}
        >
          {loading ? 'Copying…' : 'Copy Survey'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
