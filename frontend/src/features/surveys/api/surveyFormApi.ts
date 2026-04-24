import axios from 'axios';
import type { SurveyFormDto, SubmitSurveyRequest } from '../types';

// Separate axios instance — NO auth headers attached
const publicAxios = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

export const surveyFormApi = {
  getForm: (token: string): Promise<SurveyFormDto> =>
    publicAxios.get<SurveyFormDto>(`/surveys/form/${token}`).then(r => r.data),

  submit: (token: string, req: SubmitSurveyRequest): Promise<void> =>
    publicAxios.post(`/surveys/form/${token}/submit`, req).then(() => undefined),
};
