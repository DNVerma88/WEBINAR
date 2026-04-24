import { TextField } from '@/components/ui';
import { Controller } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function SummarySection({ control }: Props) {
  return (
    <Controller
      name="summary"
      control={control}
      render={({ field }) => (
        <TextField
          {...field}
          label="Professional Summary"
          multiline
          rows={5}
          fullWidth
          placeholder="Write a brief professional summary highlighting your key skills and experience..."
        />
      )}
    />
  );
}
