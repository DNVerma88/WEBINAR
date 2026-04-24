import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  LinearProgress,
  Stack,
  Typography,
} from '@mui/material';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { useRef, useState } from 'react';
import { resumeApi } from '../../talentApi';
import type { ParsedResumeImport, ResumeProfile } from '../../types';

interface ImportResumeDialogProps {
  open: boolean;
  onClose: () => void;
  /** Called with a partial ResumeProfile ready to be reset() into the form. */
  onImported: (data: Partial<ResumeProfile>) => void;
  /** Optional dialog title override — defaults to 'Import Resume from File'. */
  dialogTitle?: string;
}

const ACCEPTED_EXTENSIONS = ['.pdf', '.docx'];
const MAX_SIZE_MB = 10;
const MAX_SIZE_BYTES = MAX_SIZE_MB * 1024 * 1024;

/** Maps the AI-parsed DTO from the backend into a shape compatible with react-hook-form's ResumeProfile. */
function mapToProfle(parsed: ParsedResumeImport): Partial<ResumeProfile> {
  return {
    template: 'Professional',
    personalInfo: {
      fullName: parsed.personalInfo?.fullName ?? '',
      email:    parsed.personalInfo?.email    ?? '',
      phone:    parsed.personalInfo?.phone    ?? '',
      location: parsed.personalInfo?.location ?? '',
      linkedIn: parsed.personalInfo?.linkedIn ?? undefined,
      website:  parsed.personalInfo?.website  ?? undefined,
      headline: parsed.personalInfo?.headline ?? undefined,
    },
    summary: parsed.summary ?? '',
    workExperience: (parsed.workExperience ?? []).map((exp: ParsedResumeImport['workExperience'][number]) => ({
      id:          crypto.randomUUID(),
      jobTitle:    exp.jobTitle    ?? '',
      company:     exp.company     ?? '',
      startDate:   exp.startDate   ?? '',
      endDate:     exp.endDate     ?? undefined,
      description: exp.description ?? undefined,
    })),
    education: (parsed.education ?? []).map((edu: ParsedResumeImport['education'][number]) => ({
      id:          crypto.randomUUID(),
      degree:      edu.degree      ?? '',
      institution: edu.institution ?? '',
      startYear:   edu.startYear   ?? '',
      endYear:     edu.endYear     ?? undefined,
    })),
    skills: (parsed.skills ?? []).map((sk: ParsedResumeImport['skills'][number]) => ({
      id:    crypto.randomUUID(),
      name:  sk.name  ?? '',
      level: sk.level ?? undefined,
    })),
    certifications: (parsed.certifications ?? []).map((cert: ParsedResumeImport['certifications'][number]) => ({
      id:     crypto.randomUUID(),
      name:   cert.name   ?? '',
      issuer: cert.issuer ?? '',
      date:   cert.date   ?? undefined,
      url:    cert.url    ?? undefined,
    })),
    projects: (parsed.projects ?? []).map((proj: ParsedResumeImport['projects'][number]) => ({
      id:           crypto.randomUUID(),
      name:         proj.name         ?? '',
      company:      proj.company      ?? undefined,
      description:  proj.description  ?? undefined,
      technologies: proj.technologies ?? undefined,
      url:          proj.url          ?? undefined,
    })),
    languages: (parsed.languages ?? []).map((lang: ParsedResumeImport['languages'][number]) => ({
      id:          crypto.randomUUID(),
      name:        lang.name        ?? '',
      proficiency: lang.proficiency ?? undefined,
    })),
    publications: (parsed.publications ?? []).map((pub: ParsedResumeImport['publications'][number]) => ({
      id:      crypto.randomUUID(),
      title:   pub.title   ?? '',
      journal: pub.journal ?? undefined,
      year:    pub.year    ?? undefined,
      url:     pub.url     ?? undefined,
    })),
    achievements: (parsed.achievements ?? []).map((ach: ParsedResumeImport['achievements'][number]) => ({
      id:          crypto.randomUUID(),
      title:       ach.title       ?? '',
      year:        ach.year        ?? undefined,
      description: ach.description ?? undefined,
    })),
  };
}

export function ImportResumeDialog({ open, onClose, onImported, dialogTitle }: ImportResumeDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [isParsing, setIsParsing] = useState(false);
  const [parseError, setParseError] = useState<string | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);

  const handleClose = () => {
    if (isParsing) return; // prevent closing while parsing is running
    setSelectedFile(null);
    setValidationError(null);
    setParseError(null);
    onClose();
  };

  const validateAndSetFile = (file: File) => {
    setParseError(null);
    const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!ACCEPTED_EXTENSIONS.includes(ext)) {
      setValidationError(`Unsupported file type "${ext}". Please upload a .pdf or .docx file.`);
      setSelectedFile(null);
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      setValidationError(`File is too large (${(file.size / 1024 / 1024).toFixed(1)} MB). Maximum size is ${MAX_SIZE_MB} MB.`);
      setSelectedFile(null);
      return;
    }
    setValidationError(null);
    setSelectedFile(file);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) validateAndSetFile(file);
    // Reset input so the same file can be re-selected after clearing
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) validateAndSetFile(file);
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = () => setIsDragOver(false);

  const handleParse = async () => {
    if (!selectedFile) return;
    setIsParsing(true);
    setParseError(null);
    try {
      const parsed = await resumeApi.parseResume(selectedFile);
      const mapped  = mapToProfle(parsed);
      onImported(mapped);
      handleClose();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
        ?? 'Could not parse the resume. Please check the file and try again.';
      setParseError(msg);
    } finally {
      setIsParsing(false);
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>{dialogTitle ?? 'Import Resume from File'}</DialogTitle>

      <DialogContent>
        <Stack spacing={2} mt={1}>
          <Typography variant="body2" color="text.secondary">
            Upload a PDF or Word (.docx) resume. AI will structure and extract the information — review all fields before saving.
          </Typography>

          {/* Drop zone */}
          <Box
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onClick={() => !isParsing && fileInputRef.current?.click()}
            sx={{
              border: '2px dashed',
              borderColor: isDragOver ? 'primary.main' : 'divider',
              borderRadius: 2,
              p: 4,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 1,
              cursor: isParsing ? 'default' : 'pointer',
              bgcolor: isDragOver ? 'action.hover' : 'background.paper',
              transition: 'border-color 0.2s, background-color 0.2s',
              '&:hover': isParsing ? {} : { borderColor: 'primary.main', bgcolor: 'action.hover' },
            }}
          >
            <UploadFileIcon sx={{ fontSize: 48, color: 'text.secondary' }} />
            {selectedFile ? (
              <>
                <Typography variant="body1" fontWeight={600}>{selectedFile.name}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {(selectedFile.size / 1024).toFixed(1)} KB — click to change
                </Typography>
              </>
            ) : (
              <>
                <Typography variant="body1">Drag & drop or click to select</Typography>
                <Typography variant="body2" color="text.secondary">
                  Supported: .pdf, .docx — max {MAX_SIZE_MB} MB
                </Typography>
              </>
            )}
          </Box>

          {/* Hidden file input */}
          <input
            ref={fileInputRef}
            type="file"
            accept=".pdf,.docx"
            style={{ display: 'none' }}
            onChange={handleFileChange}
          />

          {validationError && <Alert severity="error">{validationError}</Alert>}
          {parseError      && <Alert severity="error">{parseError}</Alert>}

          {isParsing && (
            <Box>
              <LinearProgress />
              <Typography variant="caption" color="text.secondary" mt={0.5} display="block" textAlign="center">
                Parsing with AI — this may take a few seconds…
              </Typography>
            </Box>
          )}
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose} disabled={isParsing}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={() => void handleParse()}
          disabled={!selectedFile || isParsing}
          startIcon={isParsing ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {isParsing ? 'Parsing…' : 'Import'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
