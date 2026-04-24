import { useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Divider,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiErrorAlert } from '../../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { surveysApi } from '../api/surveysApi';
import { QuestionEditor } from '../components/QuestionEditor';
import type {
  SurveyDto,
  SurveyQuestionDto,
  AddSurveyQuestionRequest,
  UpdateSurveyQuestionRequest,
} from '../types';

interface QuestionsTabProps {
  survey: SurveyDto;
}

export function QuestionsTab({ survey }: QuestionsTabProps) {
  const qc = useQueryClient();
  const isDraft = survey.status === 'Draft';
  // Allow editing questions on Active surveys that have no responses yet
  const canEdit = isDraft || (survey.status === 'Active' && survey.totalResponded === 0);

  const [editorOpen, setEditorOpen] = useState(false);
  const [editingQuestion, setEditingQuestion] = useState<SurveyQuestionDto | undefined>(undefined);
  const [deleteTarget, setDeleteTarget] = useState<SurveyQuestionDto | null>(null);

  const { data: surveyDetail, isLoading, error } = useQuery({
    queryKey: ['surveys', survey.id, 'detail'],
    queryFn: () => surveysApi.getSurveyById(survey.id),
  });

  // Derive questions from results if available, otherwise empty
  const { data: results } = useQuery({
    queryKey: ['surveys', survey.id, 'results'],
    queryFn: () => surveysApi.getResults(survey.id),
    enabled: survey.status !== 'Draft',
  });

  // Use a local questions state derived from the survey detail
  const [localQuestions, setLocalQuestions] = useState<SurveyQuestionDto[]>([]);

  // Load questions from results or a separate endpoint
  const questions: SurveyQuestionDto[] = results
    ? results.questionResults.map((q, i) => ({
        id: q.questionId,
        surveyId: survey.id,
        questionText: q.questionText,
        questionType: q.questionType,
        isRequired: true,
        orderSequence: i,
        minRating: 1,
        maxRating: 5,
      }))
    : localQuestions;

  void surveyDetail;
  void isLoading;

  const invalidate = () => {
    void qc.invalidateQueries({ queryKey: ['surveys', survey.id] });
    void qc.invalidateQueries({ queryKey: ['surveys', 'list'] });
  };

  const addMut = useMutation({
    mutationFn: (req: AddSurveyQuestionRequest) => surveysApi.addQuestion(survey.id, req),
    onSuccess: (newQ) => {
      setLocalQuestions(prev => [...prev, newQ]);
      setEditorOpen(false);
      invalidate();
    },
  });

  const updateMut = useMutation({
    mutationFn: ({ qId, req }: { qId: string; req: UpdateSurveyQuestionRequest }) =>
      surveysApi.updateQuestion(survey.id, qId, req),
    onSuccess: (updated) => {
      setLocalQuestions(prev => prev.map(q => q.id === updated.id ? updated : q));
      setEditorOpen(false);
      setEditingQuestion(undefined);
      invalidate();
    },
  });

  const deleteMut = useMutation({
    mutationFn: (qId: string) => surveysApi.deleteQuestion(survey.id, qId),
    onSuccess: (_, qId) => {
      setLocalQuestions(prev => prev.filter(q => q.id !== qId));
      setDeleteTarget(null);
      invalidate();
    },
  });

  function handleSave(req: AddSurveyQuestionRequest | UpdateSurveyQuestionRequest) {
    if (editingQuestion) {
      updateMut.mutate({ qId: editingQuestion.id, req: req as UpdateSurveyQuestionRequest });
    } else {
      addMut.mutate(req as AddSurveyQuestionRequest);
    }
  }

  function openEdit(q: SurveyQuestionDto) {
    setEditingQuestion(q);
    setEditorOpen(true);
  }

  function openAdd() {
    setEditingQuestion(undefined);
    setEditorOpen(true);
  }

  if (error) return <ApiErrorAlert error={error} />;

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h6">Questions ({questions.length})</Typography>
        {isDraft && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={openAdd}>
            Add Question
          </Button>
        )}
      </Stack>

      {questions.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {isDraft
            ? 'No questions yet. Add your first question to get started.'
            : 'No questions in this survey.'}
        </Typography>
      ) : (
        <List disablePadding>
          {questions.map((q, idx) => (
            <Box key={q.id}>
              <ListItem
                disableGutters
                secondaryAction={
                  <Stack direction="row" spacing={0.5}>
                    <Tooltip title={canEdit ? 'Edit' : 'Cannot edit after responses received'}>
                      <span>
                        <IconButton
                          size="small"
                          disabled={!canEdit}
                          onClick={() => canEdit && openEdit(q)}
                        >
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title={canEdit ? 'Delete' : 'Cannot delete after responses received'}>
                      <span>
                        <IconButton
                          size="small"
                          color="error"
                          disabled={!canEdit}
                          onClick={() => canEdit && setDeleteTarget(q)}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </Stack>
                }
              >
                {isDraft && (
                  <IconButton size="small" sx={{ mr: 1, cursor: 'grab', color: 'text.disabled' }}>
                    <DragIndicatorIcon />
                  </IconButton>
                )}
                <ListItemText
                  primary={
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Typography variant="body2" color="text.secondary" sx={{ minWidth: 24 }}>
                        {idx + 1}.
                      </Typography>
                      <Typography variant="body1">{q.questionText}</Typography>
                      {q.isRequired && (
                        <Typography variant="caption" color="error">*</Typography>
                      )}
                    </Stack>
                  }
                  secondary={`${q.questionType}${q.questionType === 'Rating' ? ` (${q.minRating}–${q.maxRating})` : ''}`}
                />
              </ListItem>
              {idx < questions.length - 1 && <Divider />}
            </Box>
          ))}
        </List>
      )}

      <QuestionEditor
        open={editorOpen}
        question={editingQuestion}
        onClose={() => { setEditorOpen(false); setEditingQuestion(undefined); }}
        onSave={handleSave}
        loading={addMut.isPending || updateMut.isPending}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Question"
        message={`Delete "${deleteTarget?.questionText}"? This cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => deleteTarget && deleteMut.mutate(deleteTarget.id)}
        onCancel={() => setDeleteTarget(null)}
        loading={deleteMut.isPending}
        danger
      />

      {(addMut.isError || updateMut.isError || deleteMut.isError) && (
        <Box sx={{ mt: 2 }}>
          <ApiErrorAlert error={addMut.error ?? updateMut.error ?? deleteMut.error} />
        </Box>
      )}

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
          <CircularProgress />
        </Box>
      )}
    </Box>
  );
}
