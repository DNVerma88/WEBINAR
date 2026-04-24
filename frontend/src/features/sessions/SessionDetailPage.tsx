import {
  AddIcon,
  Box,
  Button,
  Chip,
  CommentIcon,
  DeleteIcon,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  EditIcon,
  FormControl,
  Grid,
  InputLabel,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Paper,
  Rating,
  Select,
  SendIcon,
  Stack,
  Tab,
  Tabs,
  TextField,
  ThumbUpIcon,
  Tooltip,
  Typography,
} from '@/components/ui';
import { useParams, Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { sessionsApi } from '../../shared/api/sessions';
import { tagsApi } from '../../shared/api/tags';
import { PageHeader } from '../../shared/components/PageHeader';
import { LoadingOverlay } from '../../shared/components/LoadingOverlay';
import { ApiErrorAlert } from '../../shared/components/ApiErrorAlert';
import { ConfirmDialog } from '../../shared/components/ConfirmDialog';
import {
  SessionStatus,
  DifficultyLevel,
  SessionFormat,
  MaterialType,
  UserRole,
  QuizQuestionType,
  SESSION_STATUS_LABELS,
  SESSION_FORMAT_LABELS,
  DIFFICULTY_LEVEL_LABELS,
} from '../../shared/types';
import { usePageTitle } from '../../shared/hooks/usePageTitle';
import { useAuth } from '../../shared/hooks/useAuth';
import { useToast } from '../../shared/hooks/useToast';
import type {
  SubmitSessionRatingRequest,
  AddSessionMaterialRequest,
  AddChapterRequest,
  CreateAarRequest,
  UpdateAarRequest,
  SubmitQuizAttemptRequest,
  EndorseSkillRequest,
  CreateCommentRequest,
  CreateQuizRequest,
  UpdateQuizRequest,
} from '../../shared/types';

// â”€â”€â”€ Schemas â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const ratingSchema = z.object({
  sessionScore: z.number().min(1).max(5),
  speakerScore: z.number().min(1).max(5),
  feedbackText: z.string().max(1000).optional().or(z.literal('')),
  nextSessionSuggestion: z.string().max(500).optional().or(z.literal('')),
});
type RatingFormValues = z.infer<typeof ratingSchema>;

const materialSchema = z.object({
  materialType: z.nativeEnum(MaterialType),
  title: z.string().min(1, 'Title is required').max(200),
  url: z.string().url('Must be a valid URL'),
});
type MaterialFormValues = z.infer<typeof materialSchema>;

const chapterSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  timestampSeconds: z.coerce.number().min(0, 'Must be â‰¥ 0'),
  orderSequence: z.coerce.number().min(0),
});

const aarSchema = z.object({
  whatWasPlanned: z.string().min(1).max(2000),
  whatHappened: z.string().min(1).max(2000),
  whatWentWell: z.string().min(1).max(2000),
  whatToImprove: z.string().min(1).max(2000),
  keyLessonsLearned: z.string().min(1).max(2000),
  isPublished: z.boolean(),
});
type AarFormValues = {
  whatWasPlanned: string;
  whatHappened: string;
  whatWentWell: string;
  whatToImprove: string;
  keyLessonsLearned: string;
  isPublished: boolean;
};

const commentSchema = z.object({
  content: z.string().min(1, 'Comment cannot be empty').max(2000),
});
type CommentFormValues = z.infer<typeof commentSchema>;

const materialTypeLabel: Record<MaterialType, string> = {
  [MaterialType.Slides]: 'Slides',
  [MaterialType.Document]: 'Document',
  [MaterialType.DemoLink]: 'Demo',
  [MaterialType.RecordingLink]: 'Recording',
  [MaterialType.CodeRepository]: 'Code',
  [MaterialType.FAQ]: 'FAQ',
};

// F4/F5: reject non-HTTP(S) URLs from user-supplied href attrs to prevent XSS via javascript: URIs
const safeHref = (url: string) => /^https?:\/\//i.test(url) ? url : '#';

function TabPanel({ children, value, index }: { children: React.ReactNode; value: number; index: number }) {
  return value === index ? <Box mt={2}>{children}</Box> : null;
}

export default function SessionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { user, hasRole } = useAuth();
  const toast = useToast();

  const [tab, setTab] = useState(0);
  const [showRatingDialog, setShowRatingDialog] = useState(false);
  const [showMaterialDialog, setShowMaterialDialog] = useState(false);
  const [showCancelDialog, setShowCancelDialog] = useState(false);
  const [showAddChapterDialog, setShowAddChapterDialog] = useState(false);
  const [showAarDialog, setShowAarDialog] = useState(false);
  const [confirmDeleteChapterId, setConfirmDeleteChapterId] = useState<string | null>(null);
  const [replyToCommentId, setReplyToCommentId] = useState<string | null>(null);
  const [showQuizDialog, setShowQuizDialog] = useState(false);
  const [quizTitle, setQuizTitle] = useState('');
  const [quizDesc, setQuizDesc] = useState('');
  const [quizPassPct, setQuizPassPct] = useState(70);
  const [quizAllowRetry, setQuizAllowRetry] = useState(true);
  const [quizMaxAttempts, setQuizMaxAttempts] = useState(2);
  const [quizIsActive, setQuizIsActive] = useState(true);
  const [quizQuestions, setQuizQuestions] = useState<Array<{
    questionText: string;
    questionType: QuizQuestionType;
    options: string[];
    correctAnswer: string;
    points: number;
  }>>([]);

  const { data: session, isLoading, error } = useQuery({
    queryKey: ['sessions', id],
    queryFn: ({ signal }) => sessionsApi.getSessionById(id!, signal),
    enabled: !!id,
  });

  const { data: materials } = useQuery({
    queryKey: ['sessions', id, 'materials'],
    queryFn: ({ signal }) => sessionsApi.getMaterials(id!, signal),
    enabled: !!id,
  });

  const { data: ratingsSummary } = useQuery({
    queryKey: ['sessions', id, 'ratings'],
    queryFn: ({ signal }) => sessionsApi.getRatingsSummary(id!, signal),
    enabled: !!id && session?.status === SessionStatus.Completed,
  });

  const { data: chapters } = useQuery({
    queryKey: ['sessions', id, 'chapters'],
    queryFn: ({ signal }) => sessionsApi.getChapters(id!, signal),
    enabled: !!id && tab === 1,
  });

  const { data: quiz } = useQuery({
    queryKey: ['sessions', id, 'quiz'],
    queryFn: ({ signal }) => sessionsApi.getQuiz(id!, signal),
    enabled: !!id && tab === 2,
    retry: 1,
  });

  const { data: myAttempts } = useQuery({
    queryKey: ['sessions', id, 'quiz', 'attempts'],
    queryFn: ({ signal }) => sessionsApi.getMyQuizAttempts(id!, signal),
    enabled: !!id && tab === 2,
  });

  const { data: aar } = useQuery({
    queryKey: ['sessions', id, 'aar'],
    queryFn: ({ signal }) => sessionsApi.getAar(id!, signal),
    enabled: !!id && tab === 3,
    retry: 1,
  });

  const { data: comments } = useQuery({
    queryKey: ['sessions', id, 'comments'],
    queryFn: ({ signal }) => sessionsApi.getComments(id!, signal),
    enabled: !!id && tab === 5,
  });

  const { data: tags } = useQuery({
    queryKey: ['tags'],
    queryFn: ({ signal }) => tagsApi.getTags(undefined, signal),
    enabled: tab === 4,
  });

  usePageTitle(session?.title ?? 'Session');

  const cancelSessionMutation = useMutation({
    mutationFn: () => sessionsApi.cancelSession(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['sessions', id] }); setShowCancelDialog(false); toast.success('Session cancelled.'); },
    onError: () => { setShowCancelDialog(false); toast.error('Failed to cancel session.'); },
  });

  const completeSessionMutation = useMutation({
    mutationFn: () => sessionsApi.completeSession(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['sessions', id] }); toast.success('Session marked as completed.'); },
    onError: () => toast.error('Failed to complete session.'),
  });

  const registerMutation = useMutation({
    mutationFn: () => sessionsApi.registerForSession(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['sessions', id] }); toast.success('Registered successfully!'); },
    onError: () => toast.error('Registration failed.'),
  });

  const cancelRegMutation = useMutation({
    mutationFn: () => sessionsApi.cancelRegistration(id!),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['sessions', id] }); toast.success('Registration cancelled.'); },
    onError: () => toast.error('Failed to cancel registration.'),
  });

  const submitRatingMutation = useMutation({
    mutationFn: (data: SubmitSessionRatingRequest) => sessionsApi.submitRating(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'ratings'] });
      setShowRatingDialog(false);
      resetRating();
      toast.success('Rating submitted. Thank you!');
    },
    onError: () => toast.error('Failed to submit rating.'),
  });

  const addMaterialMutation = useMutation({
    mutationFn: (data: AddSessionMaterialRequest) => sessionsApi.addMaterial(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'materials'] });
      setShowMaterialDialog(false);
      resetMaterial();
      toast.success('Material added.');
    },
    onError: () => toast.error('Failed to add material.'),
  });

  const addChapterMutation = useMutation({
    mutationFn: (data: AddChapterRequest) => sessionsApi.addChapter(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'chapters'] });
      setShowAddChapterDialog(false);
      resetChapter();
      toast.success('Chapter added.');
    },
    onError: () => toast.error('Failed to add chapter.'),
  });

  const deleteChapterMutation = useMutation({
    mutationFn: (chapterId: string) => sessionsApi.deleteChapter(id!, chapterId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'chapters'] });
      setConfirmDeleteChapterId(null);
      toast.success('Chapter deleted.');
    },
    onError: () => toast.error('Failed to delete chapter.'),
  });

  const createQuizMutation = useMutation({
    mutationFn: (data: CreateQuizRequest) => sessionsApi.createQuiz(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'quiz'] });
      setShowQuizDialog(false);
      toast.success('Quiz created.');
    },
    onError: () => toast.error('Failed to create quiz.'),
  });

  const updateQuizMutation = useMutation({
    mutationFn: (data: UpdateQuizRequest) => sessionsApi.updateQuiz(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'quiz'] });
      setShowQuizDialog(false);
      toast.success('Quiz updated.');
    },
    onError: () => toast.error('Failed to update quiz.'),
  });

  const openQuizDialog = () => {
    if (quiz) {
      setQuizTitle(quiz.title);
      setQuizDesc(quiz.description ?? '');
      setQuizPassPct(quiz.passingThresholdPercent);
      setQuizAllowRetry(quiz.allowRetry);
      setQuizMaxAttempts(quiz.maxAttempts);
      setQuizIsActive(quiz.isActive);
      setQuizQuestions(
        quiz.questions.map((q) => ({
          questionText: q.questionText,
          questionType: q.questionType,
          options: q.options ?? [],
          correctAnswer: q.correctAnswer ?? '',
          points: q.points,
        }))
      );
    } else {
      setQuizTitle('');
      setQuizDesc('');
      setQuizPassPct(70);
      setQuizAllowRetry(true);
      setQuizMaxAttempts(2);
      setQuizIsActive(true);
      setQuizQuestions([]);
    }
    setShowQuizDialog(true);
  };

  const handleQuizSave = () => {
    const payload = {
      title: quizTitle,
      description: quizDesc || undefined,
      passingThresholdPercent: quizPassPct,
      allowRetry: quizAllowRetry,
      maxAttempts: quizMaxAttempts,
      questions: quizQuestions.map((q, i) => ({ ...q, orderSequence: i })),
    };
    if (quiz) {
      updateQuizMutation.mutate({ ...payload, isActive: quizIsActive });
    } else {
      createQuizMutation.mutate(payload);
    }
  };

  const [quizAnswers, setQuizAnswers] = useState<Record<string, string>>({});
  const submitQuizMutation = useMutation({
    mutationFn: (data: SubmitQuizAttemptRequest) => sessionsApi.submitQuizAttempt(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'quiz', 'attempts'] });
      setQuizAnswers({});
      toast.success('Quiz submitted!');
    },
    onError: () => toast.error('Failed to submit quiz.'),
  });

  const createAarMutation = useMutation({
    mutationFn: (data: CreateAarRequest) => sessionsApi.createAar(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'aar'] });
      setShowAarDialog(false);
      toast.success('After-action review saved.');
    },
    onError: () => toast.error('Failed to save after-action review.'),
  });

  const updateAarMutation = useMutation({
    mutationFn: (data: UpdateAarRequest) => sessionsApi.updateAar(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'aar'] });
      setShowAarDialog(false);
      toast.success('After-action review updated.');
    },
    onError: () => toast.error('Failed to update after-action review.'),
  });

  const [endorseTagId, setEndorseTagId] = useState('');
  const endorseMutation = useMutation({
    mutationFn: (data: EndorseSkillRequest) => sessionsApi.endorseSkill(id!, data),
    onSuccess: () => { setEndorseTagId(''); toast.success('Skill endorsed!'); },
    onError: () => toast.error('Failed to endorse skill.'),
  });

  const addCommentMutation = useMutation({
    mutationFn: (data: CreateCommentRequest) => sessionsApi.addComment(id!, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sessions', id, 'comments'] });
      resetComment();
      setReplyToCommentId(null);
      toast.success('Comment posted.');
    },
    onError: () => toast.error('Failed to post comment.'),
  });

  const deleteCommentMutation = useMutation({
    mutationFn: (commentId: string) => sessionsApi.deleteComment(id!, commentId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['sessions', id, 'comments'] }); toast.success('Comment deleted.'); },
    onError: () => toast.error('Failed to delete comment.'),
  });

  const likeCommentMutation = useMutation({
    mutationFn: (commentId: string) => sessionsApi.likeComment(commentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['sessions', id, 'comments'] }),
  });

  const {
    handleSubmit: handleRatingSubmit,
    control: ratingControl,
    register: ratingRegister,
    reset: resetRating,
    formState: { isSubmitting: isRatingSubmitting },
  } = useForm<RatingFormValues>({
    resolver: zodResolver(ratingSchema),
    defaultValues: { sessionScore: 5, speakerScore: 5 },
  });

  const {
    handleSubmit: handleMaterialSubmit,
    control: materialControl,
    register: materialRegister,
    reset: resetMaterial,
    formState: { errors: materialErrors, isSubmitting: isMaterialSubmitting },
  } = useForm<z.input<typeof materialSchema>, unknown, MaterialFormValues>({
    resolver: zodResolver(materialSchema),
    defaultValues: { materialType: MaterialType.Slides },
  });

  const {
    handleSubmit: handleChapterSubmit,
    register: chapterRegister,
    reset: resetChapter,
    formState: { errors: chapterErrors, isSubmitting: isChapterSubmitting },
  } = useForm({ resolver: zodResolver(chapterSchema), defaultValues: { title: '', timestampSeconds: 0, orderSequence: 0 } });

  const {
    handleSubmit: handleAarSubmit,
    register: aarRegister,
    reset: resetAar,
    setValue: setAarValue,
    formState: { errors: aarErrors, isSubmitting: isAarSubmitting },
  } = useForm<AarFormValues>({ resolver: zodResolver(aarSchema) });

  const {
    handleSubmit: handleCommentSubmit,
    register: commentRegister,
    reset: resetComment,
    formState: { isSubmitting: isCommentSubmitting },
  } = useForm<CommentFormValues>({ resolver: zodResolver(commentSchema) });

  const isAdmin = hasRole(UserRole.Admin) || hasRole(UserRole.SuperAdmin);
  const canEdit = isAdmin || hasRole(UserRole.KnowledgeTeam) || session?.speakerId === user?.userId;
  const canCancelSession = isAdmin || hasRole(UserRole.KnowledgeTeam);
  const canRate = session?.status === SessionStatus.Completed && session?.isRegistered;
  const isSpeaker = session?.speakerId === user?.userId;
  const canWriteAar = isSpeaker || canEdit;

  if (isLoading) return <LoadingOverlay />;
  if (!session) return <Typography>Session not found.</Typography>;

  const onRatingSubmit = (data: RatingFormValues) => {
    submitRatingMutation.mutate({
      sessionScore: data.sessionScore,
      speakerScore: data.speakerScore,
      feedbackText: data.feedbackText || undefined,
      nextSessionSuggestion: data.nextSessionSuggestion || undefined,
    });
  };

  const onMaterialSubmit = (data: MaterialFormValues) => {
    addMaterialMutation.mutate({ materialType: data.materialType as MaterialType, title: data.title, url: data.url });
  };

  const onChapterSubmit = (data: { title: string; timestampSeconds: number; orderSequence: number }) => {
    addChapterMutation.mutate(data);
  };

  const onAarSubmit = (data: AarFormValues) => {
    if (aar) {
      updateAarMutation.mutate(data);
    } else {
      createAarMutation.mutate(data);
    }
  };

  const openAarDialog = () => {
    if (aar) {
      setAarValue('whatWasPlanned', aar.whatWasPlanned);
      setAarValue('whatHappened', aar.whatHappened);
      setAarValue('whatWentWell', aar.whatWentWell);
      setAarValue('whatToImprove', aar.whatToImprove);
      setAarValue('keyLessonsLearned', aar.keyLessonsLearned);
      setAarValue('isPublished', aar.isPublished);
    } else {
      resetAar();
    }
    setShowAarDialog(true);
  };

  const onCommentSubmit = (data: CommentFormValues) => {
    addCommentMutation.mutate({ content: data.content, parentCommentId: replyToCommentId ?? undefined });
  };

  const handleSubmitQuiz = () => {
    if (!quiz) return;
    const answers = quiz.questions.map((q) => ({ questionId: q.id, answer: quizAnswers[q.id] ?? '' }));
    submitQuizMutation.mutate({ answers });
  };

  return (
    <Box>
      <PageHeader
        title={session.title}
        breadcrumbs={[{ label: 'Sessions', to: '/sessions' }, { label: session.title }]}
        actions={
          <Stack direction="row" spacing={1}>
            {canEdit && (
              <Button variant="outlined" startIcon={<AddIcon />} onClick={() => setShowMaterialDialog(true)}>
                Add Material
              </Button>
            )}
            {canEdit && (
              <Button variant="outlined" startIcon={<EditIcon />} component={Link} to={`/sessions/${id}/edit`}>
                Edit
              </Button>
            )}
            {canCancelSession && session.status === SessionStatus.Scheduled && (
              <Button variant="outlined" color="error" onClick={() => setShowCancelDialog(true)}>
                Cancel Session
              </Button>
            )}
            {canCancelSession && (session.status === SessionStatus.Scheduled || session.status === SessionStatus.InProgress) && (
              <Button
                variant="contained"
                color="success"
                disabled={completeSessionMutation.isPending}
                onClick={() => completeSessionMutation.mutate()}
              >
                Mark as Completed
              </Button>
            )}
          </Stack>
        }
      />

      <ApiErrorAlert error={error} />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Paper sx={{ p: 0 }}>
            <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" scrollButtons="auto">
              <Tab label="Overview" />
              <Tab label="Chapters" />
              <Tab label="Quiz" />
              <Tab label="After-Action Review" />
              <Tab label="Endorse Skills" />
              <Tab label="Comments" />
              <Tab label="Ratings" />
            </Tabs>

            <Box px={3} pb={3}>
              {/* Tab 0: Overview */}
              <TabPanel value={tab} index={0}>
                <Stack direction="row" spacing={1} mb={2} flexWrap="wrap">
                  <Chip label={SESSION_STATUS_LABELS[session.status as SessionStatus] ?? session.status} size="small" />
                  <Chip label={SESSION_FORMAT_LABELS[session.format as SessionFormat] ?? session.format} size="small" variant="outlined" />
                  <Chip label={DIFFICULTY_LEVEL_LABELS[session.difficultyLevel as DifficultyLevel] ?? session.difficultyLevel} size="small" variant="outlined" />
                  {session.tags.map((tag) => <Chip key={tag} label={tag} size="small" variant="outlined" />)}
                </Stack>

                {session.description && (
                  <>
                    <Typography variant="subtitle1" fontWeight={600} gutterBottom>About this session</Typography>
                    <Typography variant="body2" color="text.secondary" mb={2}>{session.description}</Typography>
                  </>
                )}

                <Divider sx={{ my: 2 }} />
                <Typography variant="body2"><strong>Speaker:</strong> {session.speakerName}</Typography>
                <Typography variant="body2"><strong>Category:</strong> {session.categoryName}</Typography>
                <Typography variant="body2"><strong>Duration:</strong> {session.durationMinutes} minutes</Typography>
                <Typography variant="body2">
                  <strong>Date & Time:</strong> {new Date(session.scheduledAt).toLocaleString()}
                </Typography>
                {session.meetingLink && (
                  <Typography variant="body2">
                    <strong>Meeting Link:</strong>{' '}
                    <a href={safeHref(session.meetingLink)} target="_blank" rel="noreferrer">Join</a>
                  </Typography>
                )}

                {materials && materials.length > 0 && (
                  <>
                    <Divider sx={{ my: 2 }} />
                    <Typography variant="subtitle1" fontWeight={600} gutterBottom>Materials</Typography>
                    <List dense>
                      {materials.map((m) => (
                        <ListItem key={m.id} disablePadding>
                          <ListItemText
                            primary={<a href={safeHref(m.url)} target="_blank" rel="noreferrer">{m.title}</a>}
                            secondary={materialTypeLabel[m.materialType]}
                          />
                        </ListItem>
                      ))}
                    </List>
                  </>
                )}
              </TabPanel>

              {/* Tab 1: Chapters */}
              <TabPanel value={tab} index={1}>
                <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
                  <Typography variant="h6">Session Chapters</Typography>
                  {canEdit && (
                    <Button variant="contained" size="small" startIcon={<AddIcon />} onClick={() => setShowAddChapterDialog(true)}>
                      Add Chapter
                    </Button>
                  )}
                </Stack>
                {chapters && chapters.length > 0 ? (
                  <List dense>
                    {[...chapters].sort((a, b) => a.orderSequence - b.orderSequence).map((ch) => (
                      <ListItem key={ch.id} secondaryAction={
                        canEdit && (
                          <Tooltip title="Delete chapter">
                            <Button size="small" color="error" onClick={() => setConfirmDeleteChapterId(ch.id)}>
                              <DeleteIcon fontSize="small" />
                            </Button>
                          </Tooltip>
                        )
                      }>
                        <ListItemText
                          primary={ch.title}
                          secondary={`${Math.floor(ch.timestampSeconds / 60)}:${String(ch.timestampSeconds % 60).padStart(2, '0')}`}
                        />
                      </ListItem>
                    ))}
                  </List>
                ) : (
                  <Typography variant="body2" color="text.secondary">No chapters added yet.</Typography>
                )}
              </TabPanel>

              {/* Tab 2: Quiz */}
              <TabPanel value={tab} index={2}>
                {canEdit && (
                  <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
                    <Typography variant="h6">Session Quiz</Typography>
                    <Button
                      variant="contained"
                      size="small"
                      startIcon={quiz ? <EditIcon /> : <AddIcon />}
                      onClick={openQuizDialog}
                    >
                      {quiz ? 'Edit Quiz' : 'Create Quiz'}
                    </Button>
                  </Stack>
                )}
                {!quiz ? (
                  <Typography variant="body2" color="text.secondary">No quiz available for this session.</Typography>
                ) : (
                  <Box>
                    <Typography variant="h6" gutterBottom>{quiz.title}</Typography>
                    {quiz.description && <Typography variant="body2" color="text.secondary" mb={2}>{quiz.description}</Typography>}
                    <Typography variant="caption" color="text.secondary" display="block" mb={2}>
                      Passing threshold: {quiz.passingThresholdPercent}% Â·{' '}
                      {quiz.allowRetry ? `Up to ${quiz.maxAttempts === 0 ? 'âˆž' : quiz.maxAttempts} attempts` : 'Single attempt'}
                    </Typography>

                    {myAttempts && myAttempts.length > 0 && (
                      <Box mb={3}>
                        <Typography variant="subtitle2" gutterBottom>Your Attempts</Typography>
                        {myAttempts.map((a) => (
                          <Box key={a.id} display="flex" gap={2} alignItems="center" mb={0.5}>
                            <Typography variant="body2">Attempt {a.attemptNumber}</Typography>
                            {a.score !== undefined && <Typography variant="body2">Score: {a.score}%</Typography>}
                            {a.isPassed !== undefined && (
                              <Chip label={a.isPassed ? 'Passed' : 'Failed'} size="small" color={a.isPassed ? 'success' : 'error'} />
                            )}
                          </Box>
                        ))}
                      </Box>
                    )}

                    <ApiErrorAlert error={submitQuizMutation.error} />
                    {submitQuizMutation.data && (
                      <Box mb={2}>
                        <Chip
                          label={submitQuizMutation.data.isPassed
                            ? `Passed! Score: ${submitQuizMutation.data.score}%`
                            : `Score: ${submitQuizMutation.data.score}% â€” Try again`}
                          color={submitQuizMutation.data.isPassed ? 'success' : 'warning'}
                        />
                        {submitQuizMutation.data.xpAwarded && <Chip label="XP Awarded!" color="secondary" sx={{ ml: 1 }} />}
                      </Box>
                    )}

                    {quiz.questions.map((q, i) => (
                      <Box key={q.id} mb={3}>
                        <Typography variant="subtitle2" gutterBottom>
                          Q{i + 1}. {q.questionText}
                          <Chip label={`${q.points} pt${q.points !== 1 ? 's' : ''}`} size="small" sx={{ ml: 1 }} />
                        </Typography>

                        {q.questionType === QuizQuestionType.TrueFalse && (
                          <Stack direction="row" spacing={1}>
                            {['True', 'False'].map((opt) => (
                              <Button
                                key={opt}
                                variant={quizAnswers[q.id] === opt ? 'contained' : 'outlined'}
                                size="small"
                                onClick={() => setQuizAnswers((prev) => ({ ...prev, [q.id]: opt }))}
                              >
                                {opt}
                              </Button>
                            ))}
                          </Stack>
                        )}

                        {q.questionType === QuizQuestionType.MultipleChoice && q.options && (
                          <Stack spacing={0.5}>
                            {q.options.map((opt) => (
                              <Button
                                key={opt}
                                variant={quizAnswers[q.id] === opt ? 'contained' : 'outlined'}
                                size="small"
                                fullWidth
                                sx={{ justifyContent: 'flex-start' }}
                                onClick={() => setQuizAnswers((prev) => ({ ...prev, [q.id]: opt }))}
                              >
                                {opt}
                              </Button>
                            ))}
                          </Stack>
                        )}

                        {q.questionType === QuizQuestionType.ShortText && (
                          <TextField
                            value={quizAnswers[q.id] ?? ''}
                            onChange={(e) => setQuizAnswers((prev) => ({ ...prev, [q.id]: e.target.value }))}
                            fullWidth
                            size="small"
                            placeholder="Your answer..."
                          />
                        )}
                      </Box>
                    ))}

                    <Button variant="contained" onClick={handleSubmitQuiz} disabled={submitQuizMutation.isPending}>
                      Submit Answers
                    </Button>
                  </Box>
                )}
              </TabPanel>

              {/* Tab 3: AAR */}
              <TabPanel value={tab} index={3}>
                <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
                  <Typography variant="h6">After-Action Review</Typography>
                  {canWriteAar && (
                    <Button variant="contained" size="small" startIcon={aar ? <EditIcon /> : <AddIcon />} onClick={openAarDialog}>
                      {aar ? 'Edit AAR' : 'Write AAR'}
                    </Button>
                  )}
                </Stack>
                {aar ? (
                  <Box>
                    <Chip label={aar.isPublished ? 'Published' : 'Draft'} size="small" color={aar.isPublished ? 'success' : 'default'} sx={{ mb: 2 }} />
                    {([
                      ['What Was Planned', aar.whatWasPlanned],
                      ['What Happened', aar.whatHappened],
                      ['What Went Well', aar.whatWentWell],
                      ['What To Improve', aar.whatToImprove],
                      ['Key Lessons Learned', aar.keyLessonsLearned],
                    ] as Array<[string, string]>).map(([label, value]) => (
                      <Box key={label} mb={2}>
                        <Typography variant="subtitle2" color="text.secondary">{label}</Typography>
                        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>{value}</Typography>
                      </Box>
                    ))}
                    <Typography variant="caption" color="text.secondary">By {aar.authorName}</Typography>
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">No AAR submitted yet for this session.</Typography>
                )}
              </TabPanel>

              {/* Tab 4: Endorse Skills */}
              <TabPanel value={tab} index={4}>
                <Typography variant="h6" gutterBottom>Endorse Speaker Skills</Typography>
                <Typography variant="body2" color="text.secondary" mb={2}>
                  Recognise a skill demonstrated by the speaker during this session.
                </Typography>
                <ApiErrorAlert error={endorseMutation.error} />
                {endorseMutation.isSuccess && <Chip label="Endorsement submitted!" color="success" sx={{ mb: 2 }} />}
                <Stack direction="row" spacing={2} alignItems="center">
                  <FormControl size="small" sx={{ minWidth: 220 }}>
                    <InputLabel>Select Tag / Skill</InputLabel>
                    <Select value={endorseTagId} label="Select Tag / Skill" onChange={(e) => setEndorseTagId(e.target.value)}>
                      {(tags?.data ?? []).map((t) => (
                        <MenuItem key={t.id} value={t.id}>{t.name}</MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  <Button
                    variant="contained"
                    disabled={!endorseTagId || endorseMutation.isPending}
                    onClick={() => endorseMutation.mutate({ endorseeId: session.speakerId, tagId: endorseTagId })}
                  >
                    Endorse
                  </Button>
                </Stack>
              </TabPanel>

              {/* Tab 5: Comments */}
              <TabPanel value={tab} index={5}>
                <Typography variant="h6" gutterBottom>
                  <CommentIcon fontSize="small" sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Comments
                </Typography>
                <ApiErrorAlert error={addCommentMutation.error} />

                <Box component="form" onSubmit={handleCommentSubmit(onCommentSubmit)} mb={3}>
                  {replyToCommentId && (
                    <Typography variant="caption" color="primary" mb={0.5} display="block">
                      Replying to comment Â·{' '}
                      <Button size="small" onClick={() => setReplyToCommentId(null)}>Cancel reply</Button>
                    </Typography>
                  )}
                  <Stack direction="row" spacing={1}>
                    <TextField
                      {...commentRegister('content')}
                      placeholder={replyToCommentId ? 'Write a reply...' : 'Write a comment...'}
                      size="small"
                      fullWidth
                      multiline
                      maxRows={4}
                    />
                    <Button type="submit" variant="contained" disabled={isCommentSubmitting || addCommentMutation.isPending} sx={{ minWidth: 48, px: 1.5 }}>
                      <SendIcon />
                    </Button>
                  </Stack>
                </Box>

                {comments && comments.length > 0 ? (
                  comments.map((c) => (
                    <Box key={c.id} mb={2}>
                      <Paper variant="outlined" sx={{ p: 1.5 }}>
                        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                          <Box flex={1}>
                            <Typography variant="caption" fontWeight={600}>{c.authorName}</Typography>
                            <Typography variant="caption" color="text.secondary" ml={1}>
                              {new Date(c.createdDate).toLocaleDateString()}
                            </Typography>
                            {c.isDeleted ? (
                              <Typography variant="body2" color="text.disabled" fontStyle="italic">[deleted]</Typography>
                            ) : (
                              <Typography variant="body2" mt={0.5}>{c.content}</Typography>
                            )}
                          </Box>
                          {!c.isDeleted && (
                            <Stack direction="row" spacing={0.5} ml={1}>
                              <Tooltip title={`${c.likeCount} likes`}>
                                <Button size="small" onClick={() => likeCommentMutation.mutate(c.id)} sx={{ minWidth: 36 }}>
                                  <ThumbUpIcon fontSize="small" sx={{ color: c.hasLiked ? 'primary.main' : 'inherit' }} />
                                  <Typography variant="caption" ml={0.5}>{c.likeCount}</Typography>
                                </Button>
                              </Tooltip>
                              <Button size="small" onClick={() => setReplyToCommentId(c.id)}>Reply</Button>
                              {(isAdmin || c.authorId === user?.userId) && (
                                <Tooltip title="Delete comment">
                                  <Button size="small" color="error" onClick={() => deleteCommentMutation.mutate(c.id)}>
                                    <DeleteIcon fontSize="small" />
                                  </Button>
                                </Tooltip>
                              )}
                            </Stack>
                          )}
                        </Stack>
                      </Paper>
                      {c.replies && c.replies.length > 0 && (
                        <Box ml={4} mt={1}>
                          {c.replies.map((r) => (
                            <Paper key={r.id} variant="outlined" sx={{ p: 1.5, mb: 1 }}>
                              <Typography variant="caption" fontWeight={600}>{r.authorName}</Typography>
                              <Typography variant="caption" color="text.secondary" ml={1}>
                                {new Date(r.createdDate).toLocaleDateString()}
                              </Typography>
                              {r.isDeleted ? (
                                <Typography variant="body2" color="text.disabled" fontStyle="italic">[deleted]</Typography>
                              ) : (
                                <Typography variant="body2" mt={0.5}>{r.content}</Typography>
                              )}
                            </Paper>
                          ))}
                        </Box>
                      )}
                    </Box>
                  ))
                ) : (
                  <Typography variant="body2" color="text.secondary">No comments yet. Be the first to comment!</Typography>
                )}
              </TabPanel>

              {/* Tab 6: Ratings */}
              <TabPanel value={tab} index={6}>
                <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
                  <Typography variant="h6">Ratings & Feedback</Typography>
                  {canRate && (
                    <Button variant="contained" size="small" onClick={() => setShowRatingDialog(true)}>
                      Rate This Session
                    </Button>
                  )}
                </Stack>
                {session.status !== SessionStatus.Completed ? (
                  <Typography variant="body2" color="text.secondary">Ratings become available after the session ends.</Typography>
                ) : ratingsSummary && ratingsSummary.totalRatings > 0 ? (
                  <Stack spacing={2}>
                    <Stack direction="row" spacing={4} flexWrap="wrap">
                      <Box>
                        <Typography variant="caption" color="text.secondary">Session Score</Typography>
                        <Box display="flex" alignItems="center" gap={1}>
                          <Rating value={ratingsSummary.averageSessionScore} precision={0.5} readOnly />
                          <Typography variant="body2" fontWeight={600}>{ratingsSummary.averageSessionScore.toFixed(1)} / 5</Typography>
                        </Box>
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">Speaker Score</Typography>
                        <Box display="flex" alignItems="center" gap={1}>
                          <Rating value={ratingsSummary.averageSpeakerScore} precision={0.5} readOnly />
                          <Typography variant="body2" fontWeight={600}>{ratingsSummary.averageSpeakerScore.toFixed(1)} / 5</Typography>
                        </Box>
                      </Box>
                      <Box>
                        <Typography variant="caption" color="text.secondary">Total Ratings</Typography>
                        <Typography variant="body1" fontWeight={600}>{ratingsSummary.totalRatings}</Typography>
                      </Box>
                    </Stack>
                  </Stack>
                ) : (
                  <Typography variant="body2" color="text.secondary">No ratings yet for this session.</Typography>
                )}
              </TabPanel>
            </Box>
          </Paper>
        </Grid>

        {/* Sidebar */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Registration</Typography>
            <Typography variant="body2" color="text.secondary" mb={1}>
              {session.registeredCount} registered
              {session.participantLimit ? ` / ${session.participantLimit} max` : ''}
            </Typography>
            {(session.waitlistCount ?? 0) > 0 && (
              <Typography variant="body2" color="text.secondary" mb={1}>
                {session.waitlistCount} on waitlist
              </Typography>
            )}
            <ApiErrorAlert error={registerMutation.error} />
            <ApiErrorAlert error={cancelRegMutation.error} />

            {session.status === SessionStatus.Scheduled && (
              session.isRegistered ? (
                <Button variant="outlined" color="error" fullWidth
                  onClick={() => cancelRegMutation.mutate()} disabled={cancelRegMutation.isPending}>
                  Cancel Registration
                </Button>
              ) : (
                <Button variant="contained" fullWidth
                  onClick={() => registerMutation.mutate()} disabled={registerMutation.isPending}>
                  Register
                </Button>
              )
            )}
          </Paper>
        </Grid>
      </Grid>

      {/* Rating Dialog */}
      <Dialog open={showRatingDialog} onClose={() => setShowRatingDialog(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleRatingSubmit(onRatingSubmit)} noValidate>
          <DialogTitle>Rate Session</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={submitRatingMutation.error} />
            <Box mb={2}>
              <Typography variant="body2" gutterBottom>Session Score</Typography>
              <Controller name="sessionScore" control={ratingControl}
                render={({ field }) => (
                  <Rating value={field.value} onChange={(_, v) => field.onChange(v ?? 1)} size="large" />
                )} />
            </Box>
            <Box mb={2}>
              <Typography variant="body2" gutterBottom>Speaker Score</Typography>
              <Controller name="speakerScore" control={ratingControl}
                render={({ field }) => (
                  <Rating value={field.value} onChange={(_, v) => field.onChange(v ?? 1)} size="large" />
                )} />
            </Box>
            <TextField {...ratingRegister('feedbackText')} label="Feedback (optional)" multiline rows={3} fullWidth sx={{ mb: 2 }} />
            <TextField {...ratingRegister('nextSessionSuggestion')} label="Suggested next topic (optional)" fullWidth />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowRatingDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isRatingSubmitting || submitRatingMutation.isPending}>
              Submit Rating
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Add Material Dialog */}
      <Dialog open={showMaterialDialog} onClose={() => setShowMaterialDialog(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleMaterialSubmit(onMaterialSubmit)} noValidate>
          <DialogTitle>Add Material</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={addMaterialMutation.error} />
            <FormControl fullWidth sx={{ mt: 1, mb: 2 }}>
              <InputLabel>Type</InputLabel>
              <Controller name="materialType" control={materialControl}
                render={({ field }) => (
                  <Select {...field} label="Type">
                    {Object.values(MaterialType).map((v) => (
                      <MenuItem key={v} value={v}>{materialTypeLabel[v as MaterialType] ?? v}</MenuItem>
                    ))}
                  </Select>
                )} />
            </FormControl>
            <TextField {...materialRegister('title')} label="Title" fullWidth
              error={!!materialErrors.title} helperText={materialErrors.title?.message} sx={{ mb: 2 }} />
            <TextField {...materialRegister('url')} label="URL" fullWidth
              error={!!materialErrors.url} helperText={materialErrors.url?.message} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowMaterialDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isMaterialSubmitting || addMaterialMutation.isPending}>Add</Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Add Chapter Dialog */}
      <Dialog open={showAddChapterDialog} onClose={() => setShowAddChapterDialog(false)} maxWidth="xs" fullWidth>
        <form onSubmit={handleChapterSubmit(onChapterSubmit)} noValidate>
          <DialogTitle>Add Chapter</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={addChapterMutation.error} />
            <TextField {...chapterRegister('title')} label="Chapter Title" fullWidth
              error={!!chapterErrors.title} helperText={chapterErrors.title?.message as string} sx={{ mt: 1, mb: 2 }} />
            <TextField {...chapterRegister('timestampSeconds')} label="Timestamp (seconds)" type="number" fullWidth
              error={!!chapterErrors.timestampSeconds} helperText={chapterErrors.timestampSeconds?.message as string} sx={{ mb: 2 }} />
            <TextField {...chapterRegister('orderSequence')} label="Order" type="number" fullWidth />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowAddChapterDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isChapterSubmitting || addChapterMutation.isPending}>Add</Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* AAR Dialog */}
      <Dialog open={showAarDialog} onClose={() => setShowAarDialog(false)} maxWidth="md" fullWidth>
        <form onSubmit={handleAarSubmit(onAarSubmit)} noValidate>
          <DialogTitle>{aar ? 'Edit After-Action Review' : 'Write After-Action Review'}</DialogTitle>
          <DialogContent>
            <ApiErrorAlert error={createAarMutation.error ?? updateAarMutation.error} />
            {(
              [
                ['whatWasPlanned', 'What Was Planned'],
                ['whatHappened', 'What Happened'],
                ['whatWentWell', 'What Went Well'],
                ['whatToImprove', 'What To Improve'],
                ['keyLessonsLearned', 'Key Lessons Learned'],
              ] as Array<[keyof AarFormValues, string]>
            ).map(([field, label]) => (
              <TextField
                key={field}
                {...aarRegister(field)}
                label={label}
                multiline
                rows={3}
                fullWidth
                error={!!aarErrors[field]}
                helperText={aarErrors[field]?.message as string}
                sx={{ mb: 2, mt: field === 'whatWasPlanned' ? 1 : 0 }}
              />
            ))}
            <FormControl size="small">
              <Stack direction="row" spacing={1} alignItems="center">
                <input type="checkbox" id="aar-published" {...aarRegister('isPublished')} />
                <label htmlFor="aar-published">Publish (visible to all)</label>
              </Stack>
            </FormControl>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowAarDialog(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isAarSubmitting || createAarMutation.isPending || updateAarMutation.isPending}>
              {aar ? 'Update AAR' : 'Submit AAR'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* Delete Chapter Confirm */}
      <ConfirmDialog
        open={!!confirmDeleteChapterId}
        title="Delete Chapter"
        message="Are you sure you want to delete this chapter?"
        confirmLabel="Delete"
        onConfirm={() => confirmDeleteChapterId && deleteChapterMutation.mutate(confirmDeleteChapterId)}
        onCancel={() => setConfirmDeleteChapterId(null)}
        loading={deleteChapterMutation.isPending}
        danger
      />

      {/* Quiz Management Dialog */}
      <Dialog open={showQuizDialog} onClose={() => setShowQuizDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>{quiz ? 'Edit Quiz' : 'Create Quiz'}</DialogTitle>
        <DialogContent>
          <ApiErrorAlert error={createQuizMutation.error ?? updateQuizMutation.error} />
          <Stack spacing={2} mt={1}>
            <TextField label="Quiz Title" value={quizTitle} onChange={(e) => setQuizTitle(e.target.value)} fullWidth required />
            <TextField label="Description (optional)" value={quizDesc} onChange={(e) => setQuizDesc(e.target.value)} fullWidth multiline rows={2} />
            <Stack direction="row" spacing={2}>
              <TextField
                label="Passing Threshold (%)"
                type="number"
                value={quizPassPct}
                onChange={(e) => setQuizPassPct(Number(e.target.value))}
                sx={{ width: 180 }}
              />
              <TextField
                label="Max Attempts (0=unlimited)"
                type="number"
                value={quizMaxAttempts}
                onChange={(e) => setQuizMaxAttempts(Number(e.target.value))}
                sx={{ width: 220 }}
              />
              <Stack direction="row" alignItems="center" spacing={1}>
                <input
                  type="checkbox"
                  id="quiz-allow-retry"
                  checked={quizAllowRetry}
                  onChange={(e) => setQuizAllowRetry(e.target.checked)}
                />
                <label htmlFor="quiz-allow-retry">Allow Retry</label>
              </Stack>
              {quiz && (
                <Stack direction="row" alignItems="center" spacing={1}>
                  <input
                    type="checkbox"
                    id="quiz-active"
                    checked={quizIsActive}
                    onChange={(e) => setQuizIsActive(e.target.checked)}
                  />
                  <label htmlFor="quiz-active">Active</label>
                </Stack>
              )}
            </Stack>

            <Divider />
            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <Typography variant="subtitle1" fontWeight={600}>Questions ({quizQuestions.length})</Typography>
              <Button
                size="small"
                startIcon={<AddIcon />}
                onClick={() =>
                  setQuizQuestions((prev) => [
                    ...prev,
                    { questionText: '', questionType: QuizQuestionType.MultipleChoice, options: ['', ''], correctAnswer: '', points: 1 },
                  ])
                }
              >
                Add Question
              </Button>
            </Stack>

            {quizQuestions.map((q, qi) => (
              <Paper key={qi} variant="outlined" sx={{ p: 2 }}>
                <Stack spacing={1.5}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Typography variant="caption" fontWeight={600}>Question {qi + 1}</Typography>
                    <Button
                      size="small"
                      color="error"
                      onClick={() => setQuizQuestions((prev) => prev.filter((_, i) => i !== qi))}
                    >
                      <DeleteIcon fontSize="small" />
                    </Button>
                  </Stack>
                  <TextField
                    label="Question Text"
                    value={q.questionText}
                    onChange={(e) => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, questionText: e.target.value } : x))}
                    fullWidth
                    required
                  />
                  <Stack direction="row" spacing={2}>
                    <FormControl sx={{ minWidth: 180 }}>
                      <InputLabel>Type</InputLabel>
                      <Select
                        value={q.questionType}
                        label="Type"
                        size="small"
                        onChange={(e) => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, questionType: e.target.value as QuizQuestionType, options: e.target.value === QuizQuestionType.MultipleChoice ? ['', ''] : [] } : x))}
                      >
                        <MenuItem value={QuizQuestionType.MultipleChoice}>Multiple Choice</MenuItem>
                        <MenuItem value={QuizQuestionType.TrueFalse}>True / False</MenuItem>
                        <MenuItem value={QuizQuestionType.ShortText}>Short Text</MenuItem>
                      </Select>
                    </FormControl>
                    <TextField
                      label="Points"
                      type="number"
                      value={q.points}
                      onChange={(e) => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, points: Number(e.target.value) } : x))}
                      sx={{ width: 100 }}
                      size="small"
                    />
                    <TextField
                      label="Correct Answer"
                      value={q.correctAnswer}
                      onChange={(e) => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, correctAnswer: e.target.value } : x))}
                      size="small"
                      sx={{ flex: 1 }}
                      helperText={q.questionType === QuizQuestionType.TrueFalse ? 'Enter True or False' : 'Enter exact correct answer'}
                    />
                  </Stack>
                  {q.questionType === QuizQuestionType.MultipleChoice && (
                    <Box>
                      <Typography variant="caption" color="text.secondary">Options</Typography>
                      <Stack spacing={1} mt={0.5}>
                        {q.options.map((opt, oi) => (
                          <Stack key={oi} direction="row" spacing={1} alignItems="center">
                            <TextField
                              value={opt}
                              onChange={(e) => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, options: x.options.map((o, j) => j === oi ? e.target.value : o) } : x))}
                              size="small"
                              fullWidth
                              placeholder={`Option ${oi + 1}`}
                            />
                            <Button
                              size="small"
                              color="error"
                              disabled={q.options.length <= 2}
                              onClick={() => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, options: x.options.filter((_, j) => j !== oi) } : x))}
                            >
                              <DeleteIcon fontSize="small" />
                            </Button>
                          </Stack>
                        ))}
                        <Button
                          size="small"
                          onClick={() => setQuizQuestions((prev) => prev.map((x, i) => i === qi ? { ...x, options: [...x.options, ''] } : x))}
                        >
                          + Add Option
                        </Button>
                      </Stack>
                    </Box>
                  )}
                </Stack>
              </Paper>
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowQuizDialog(false)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!quizTitle.trim() || createQuizMutation.isPending || updateQuizMutation.isPending}
            onClick={handleQuizSave}
          >
            {quiz ? 'Save Changes' : 'Create Quiz'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Cancel Session Confirm */}
      <ConfirmDialog
        open={showCancelDialog}
        title="Cancel Session"
        message="This will cancel the session and notify all registered participants. This action cannot be undone."
        confirmLabel="Cancel Session"
        onConfirm={() => cancelSessionMutation.mutate()}
        onCancel={() => setShowCancelDialog(false)}
        loading={cancelSessionMutation.isPending}
        danger
      />
    </Box>
  );
}
