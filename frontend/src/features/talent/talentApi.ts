import axiosClient from '../../shared/api/axiosClient';
import type {
  ResumeProfile,
  ResumeProfileAdminSummary,
  ScreeningJobList,
  ScreeningJobDetail,
  ScreeningCandidate,
  CreateScreeningJobRequest,
  StorageFileRef,
  ScoringMode,
} from './types';

// ---------------------------------------------------------------------------
// Default AI scoring prompt template — mirrors ResumeScorer.DefaultPromptTemplate.
// Pre-populated in the UI so users can review and tweak before screening.
// Two literal placeholders are required: {JD_TEXT} and {RESUME_TEXT}.
// ---------------------------------------------------------------------------

export const DEFAULT_PROMPT_TEMPLATE = `You are an expert resume-to-job-description evaluator.

You will receive:
1. A job description
2. A candidate resume

Your job is to evaluate how well the candidate fits the role using only explicit evidence from the texts.

Instructions:
- Extract candidateName, email, and phone only if explicitly present in the resume. Otherwise use null.
- First identify the JD's core requirements:
  a) required technical skills
  b) required functional/domain skills
  c) required seniority/experience expectations
  d) required qualifications/certifications if explicitly stated
- Then compare the resume against those requirements using only explicit resume evidence.
- Do not assume experience from job titles alone unless the title is supported by responsibilities, tools, or achievements.
- Treat equivalent skills as matches only when equivalence is clear and standard.
- Ignore fluff, self-rating language, and generic claims unless backed by evidence.

Scoring rules:

semanticSimilarityScore (0.0-1.0):
- Based on overall alignment of role, responsibilities, domain, seniority, and demonstrated experience.
- Use 2 decimal precision.

skillsDepthScore (0-100):
- Score how strongly the resume demonstrates the JD's core required skills.
- Weight core required skills more than optional or nice-to-have skills.
- Reward practical implementation evidence more than keyword mentions.

legitimacyScore (0-100):
- Score credibility and consistency of the resume.
- Reduce score only for evidence-based concerns visible in the resume.
- Missing optional details alone are not red flags.

redFlags rules:
- Include only objective concerns supported by visible evidence.
- No speculation.
- If none, return [].

scoreSummary rules:
- 2-3 sentences only.
- Summarize fit level, strongest strengths, and the most important gap or concern if any.
- Do not mention absent contact details or unavailable data.

Output requirements:
- Return ONLY valid JSON.
- No markdown.
- No extra keys.
- Use exactly this structure:

{
  "candidateName": null,
  "email": null,
  "phone": null,
  "semanticSimilarityScore": 0.0,
  "skillsDepthScore": 0,
  "legitimacyScore": 0,
  "scoreSummary": "",
  "skillsMatched": [],
  "skillsGap": [],
  "redFlags": []
}

JOB DESCRIPTION:
{JD_TEXT}

RESUME:
{RESUME_TEXT}`;

// ---------------------------------------------------------------------------
// Helpers: the backend stores JSONB fields as serialized JSON strings.
// We must serialize objects → strings on the way out (PUT) and
// parse strings → objects on the way in (GET).
// ---------------------------------------------------------------------------

const jsonStringFields = [
  'personalInfo',
  'workExperience',
  'education',
  'skills',
  'certifications',
  'projects',
  'languages',
  'publications',
  'achievements',
] as const;

function parseJson<T>(value: unknown, fallback: T): T {
  if (typeof value === 'string') {
    try { return JSON.parse(value) as T; } catch { /* ignore */ }
  }
  if (value !== null && value !== undefined) return value as T;
  return fallback;
}

/** Shape of the resume profile JSON received from the backend (all JSON fields are serialized strings). */
interface ResumeProfileApiDto {
  id?: string;
  template?: string;
  personalInfo?: string;
  summary?: string;
  workExperience?: string;
  education?: string;
  skills?: string;
  certifications?: string;
  projects?: string;
  languages?: string;
  publications?: string;
  achievements?: string;
  updatedAt?: string;
}

/** Backend DTO (JSON strings) → frontend ResumeProfile (typed objects) */
function deserializeProfile(dto: ResumeProfileApiDto): ResumeProfile {
  return {
    id: dto.id ?? '',
    template: (dto.template as ResumeProfile['template']) ?? 'Professional',
    personalInfo: parseJson(dto.personalInfo, { fullName: '', email: '', phone: '', location: '' }),
    summary: dto.summary ?? '',
    workExperience: parseJson(dto.workExperience, []),
    education: parseJson(dto.education, []),
    skills: parseJson(dto.skills, []),
    certifications: parseJson(dto.certifications, []),
    projects: parseJson(dto.projects, []),
    languages: parseJson(dto.languages, []),
    publications: parseJson(dto.publications, []),
    achievements: parseJson(dto.achievements, []),
    updatedAt: dto.updatedAt ?? '',
  };
}

/** Frontend ResumeProfile (typed objects) → backend SaveResumeProfileRequest (JSON strings) */
function serializeProfile(data: Partial<ResumeProfile>): Record<string, unknown> {
  const out: Record<string, unknown> = { ...data };
  for (const field of jsonStringFields) {
    if (field in data) {
      const val = data[field];
      out[field] = typeof val === 'string' ? val : JSON.stringify(val ?? (field === 'personalInfo' ? {} : []));
    }
  }
  return out;
}

// Resume Builder
export const resumeApi = {
  getProfile: (): Promise<ResumeProfile | null> =>
    axiosClient
      .get('/talent/resume')
      .then((r) => deserializeProfile(r.data))
      .catch((e) => {
        if (e?.response?.status === 404) return null;
        throw e;
      }),
  saveProfile: (data: Partial<ResumeProfile>) =>
    axiosClient
      .put('/talent/resume', serializeProfile(data))
      .then((r) => deserializeProfile(r.data)),
  downloadPdf: () =>
    axiosClient
      .get<Blob>('/talent/resume/pdf', { responseType: 'blob' })
      .then((r) => r.data),
  downloadWord: () =>
    axiosClient
      .get<Blob>('/talent/resume/word', { responseType: 'blob' })
      .then((r) => r.data),
  adminListAll: () =>
    axiosClient
      .get<ResumeProfileAdminSummary[]>('/talent/resume/admin/all')
      .then((r) => r.data),
  adminDownloadPdf: (userId: string) =>
    axiosClient
      .get<Blob>(`/talent/resume/admin/${userId}/pdf`, { responseType: 'blob' })
      .then((r) => r.data),
  adminDownloadWord: (userId: string) =>
    axiosClient
      .get<Blob>(`/talent/resume/admin/${userId}/word`, { responseType: 'blob' })
      .then((r) => r.data),
  adminGetProfile: (userId: string): Promise<ResumeProfile | null> =>
    axiosClient
      .get(`/talent/resume/admin/${userId}`)
      .then((r) => deserializeProfile(r.data as ResumeProfileApiDto))
      .catch((e) => {
        if (e?.response?.status === 404) return null;
        throw e;
      }),
  adminSaveProfile: (userId: string, data: Partial<ResumeProfile>) =>
    axiosClient
      .put(`/talent/resume/admin/${userId}`, serializeProfile(data))
      .then((r) => deserializeProfile(r.data as ResumeProfileApiDto)),
  parseResume: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return axiosClient
      .post<import('./types').ParsedResumeImport>(`/talent/resume/parse`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data);
  },
};

// Screener
export const screeningApi = {
  createJob: (data: CreateScreeningJobRequest) =>
    axiosClient.post<ScreeningJobList>('/talent/screening', data).then((r) => r.data),
  getJobs: () =>
    axiosClient
      .get<{ data: ScreeningJobList[] }>('/talent/screening')
      .then((r) => (Array.isArray(r.data) ? r.data : (r.data?.data ?? []))),
  getJob: (jobId: string) =>
    axiosClient.get<ScreeningJobDetail>(`/talent/screening/${jobId}`).then((r) => r.data),
  uploadFiles: (jobId: string, files: File[], onProgress?: (pct: number) => void) => {
    const form = new FormData();
    files.forEach((f) => form.append('files', f));
    return axiosClient
      .post<void>(`/talent/screening/${jobId}/upload`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (e) =>
          onProgress?.(Math.round((e.loaded * 100) / (e.total ?? 1))),
      })
      .then((r) => r.data);
  },
  addFromStorage: (jobId: string, refs: StorageFileRef[]) =>
    axiosClient
      .post<void>(`/talent/screening/${jobId}/add-from-storage`, { files: refs })
      .then((r) => r.data),
  startScreening: (jobId: string, mode?: string, promptTemplate?: string) =>
    axiosClient
      .post<void>(`/talent/screening/${jobId}/start`, { scoringMode: mode, promptTemplate })
      .then((r) => r.data),
  getAiStatus: () =>
    axiosClient.get<import('./types').AiProviderStatus>('/talent/screening/ai-status').then((r) => r.data),
  reScreen: (jobId: string, mode: ScoringMode, overwriteAllScores = false, promptTemplate?: string) =>
    axiosClient
      .post<void>(`/talent/screening/${jobId}/re-screen`, { scoringMode: mode, overwriteAllScores, promptTemplate })
      .then((r) => r.data),
  updateJd: (jobId: string, jdText: string) =>
    axiosClient
      .patch<void>(`/talent/screening/${jobId}/jd`, { jdText })
      .then((r) => r.data),
  deleteJob: (jobId: string) =>
    axiosClient.delete<void>(`/talent/screening/${jobId}`).then((r) => r.data),
  getResults: (jobId: string) =>
    axiosClient
      .get<ScreeningCandidate[]>(`/talent/screening/${jobId}/results`)
      .then((r) => r.data),
  getCandidate: (jobId: string, candidateId: string) =>
    axiosClient
      .get<ScreeningCandidate>(`/talent/screening/${jobId}/candidates/${candidateId}`)
      .then((r) => r.data),
  extractJdText: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return axiosClient
      .post<{ text: string }>('/talent/screening/extract-jd', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data.text);
  },
  exportCsv: (jobId: string) =>
    axiosClient
      .get<Blob>(`/talent/screening/${jobId}/export-csv`, { responseType: 'blob' })
      .then((r) => r.data),
};
