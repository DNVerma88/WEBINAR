// Storage
export interface StorageFileRef {
  providerType: 'Local' | 'OneDrive' | 'SharePoint' | 'S3' | 'AzureBlob';
  fileId: string;
  fileName: string;
  fileSizeBytes: number;
  containerOrDrive?: string;
  accessToken?: string;
}

export interface StorageFileItem {
  fileId: string;
  fileName: string;
  sizeBytes: number;
  mimeType: string;
  lastModified: string;
}

export interface StorageProvider {
  type: string;
  isConfigured: boolean;
}

// Resume Builder
export interface PersonalInfo {
  fullName: string;
  email: string;
  phone: string;
  location: string;
  linkedIn?: string;
  website?: string;
  headline?: string;  // job title / tagline shown below the name
}

export interface WorkExperience {
  id: string;
  jobTitle: string;
  company: string;
  startDate: string;
  endDate?: string;
  description?: string;
}

export interface Education {
  id: string;
  degree: string;
  institution: string;
  startYear: string;
  endYear?: string;
  gpa?: string;
}

export interface Skill {
  id: string;
  name: string;
  level?: string;
}

export interface Certification {
  id: string;
  name: string;
  issuer: string;
  date?: string;
  url?: string;
}

export interface Project {
  id: string;
  name: string;
  description?: string;
  url?: string;
  technologies?: string;
  company?: string;  // company this project belongs to (used for grouping in PROJECT DETAILS)
}

export interface Language {
  id: string;
  name: string;
  proficiency?: string;
}

export interface Publication {
  id: string;
  title: string;
  journal?: string;
  year?: string;
  url?: string;
}

export interface Achievement {
  id: string;
  title: string;
  year?: string;
  description?: string;
}

export interface ResumeProfile {
  id: string;
  template: 'Professional' | 'Modern' | 'Minimal';
  personalInfo: PersonalInfo;
  summary?: string;
  workExperience: WorkExperience[];
  education: Education[];
  skills: Skill[];
  certifications: Certification[];
  projects: Project[];
  languages: Language[];
  publications: Publication[];
  achievements: Achievement[];
  updatedAt: string;
}

export interface ResumeProfileAdminSummary {
  userId: string;
  fullName: string;
  email: string;
  department?: string;
  designation?: string;
  hasProfile: boolean;
  updatedAt?: string;
}

// Resume Import — AI-parsed output returned by POST /api/talent/resume/parse
// Mirrors the resume profile shape but all fields are optional (null when not found by AI)
export interface ParsedResumeImport {
  personalInfo: {
    fullName?:  string | null;
    email?:     string | null;
    phone?:     string | null;
    location?:  string | null;
    linkedIn?:  string | null;
    website?:   string | null;
    headline?:  string | null;
  };
  summary?: string | null;
  workExperience: Array<{
    jobTitle?:    string | null;
    company?:     string | null;
    startDate?:   string | null;
    endDate?:     string | null;
    description?: string | null;
  }>;
  education: Array<{
    degree?:      string | null;
    institution?: string | null;
    startYear?:   string | null;
    endYear?:     string | null;
  }>;
  skills: Array<{ name?: string | null; level?: string | null; }>;
  certifications: Array<{
    name?:   string | null;
    issuer?: string | null;
    date?:   string | null;
    url?:    string | null;
  }>;
  projects: Array<{
    name?:         string | null;
    company?:      string | null;
    description?:  string | null;
    technologies?: string | null;
    url?:          string | null;
  }>;
  languages: Array<{ name?: string | null; proficiency?: string | null; }>;
  publications: Array<{
    title?:   string | null;
    journal?: string | null;
    year?:    string | null;
    url?:     string | null;
  }>;
  achievements: Array<{
    title?:       string | null;
    year?:        string | null;
    description?: string | null;
  }>;
}

// Screening
export type ScreeningJobStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';
export type CandidateStatus = 'Queued' | 'Processing' | 'Scored' | 'Failed';
export type Recommendation = 'StrongFit' | 'GoodFit' | 'MaybeFit' | 'NoFit';

export interface ScreeningJobList {
  id: string;
  jobTitle: string;
  status: ScreeningJobStatus;
  totalCandidates: number;
  processedCandidates: number;
  progressPercent: number;
  createdAt: string;
}

export interface ScreeningCandidate {
  id: string;
  fileName: string;
  storageProviderType: string;
  status: CandidateStatus;
  errorMessage?: string;
  candidateName?: string;
  email?: string;
  phone?: string;
  semanticSimilarityScore?: number;
  skillsDepthScore?: number;
  legitimacyScore?: number;
  overallScore?: number;
  recommendation?: Recommendation;
  scoreSummary?: string;
  skillsMatched?: string[];
  skillsGap?: string[];
  redFlags?: string[];
  scoredAt?: string;
}

export interface ScreeningJobDetail {
  id: string;
  jobTitle: string;
  jdText?: string;
  promptTemplate?: string;
  status: ScreeningJobStatus;
  totalCandidates: number;
  processedCandidates: number;
  progressPercent: number;
  errorMessage?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  candidates: ScreeningCandidate[];
}

export interface CreateScreeningJobRequest {
  jobTitle: string;
  jdText?: string;
  jdFileReference?: StorageFileRef;
  promptTemplate?: string;
}

// SignalR progress events
export interface ScreeningProgressEvent {
  jobId: string;
  processed: number;
  total: number;
  percentComplete: number;
  latestCandidate?: {
    candidateId: string;
    fileName: string;
    overallScore?: number;
    recommendation?: string;
  };
}

export interface AiProviderStatus {
  openAiConfigured: boolean;
  geminiConfigured: boolean;
  openAiModel: string;
  geminiModel: string;
}

// Scoring mode constants — single source of truth; mirrors backend ScoringModes.cs
export const SCORING_MODES = {
  AI:     'AI',
  Gemini: 'Gemini',
  Stub:   'Stub',
} as const;

export type ScoringMode = typeof SCORING_MODES[keyof typeof SCORING_MODES];

