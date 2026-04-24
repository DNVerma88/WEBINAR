import { Grid, TextField } from '@/components/ui';
import { Controller } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function PersonalInfoSection({ control }: Props) {
  return (
    <Grid container spacing={2}>
      <Grid size={{ xs: 12, sm: 6 }}>
        <Controller
          name="personalInfo.fullName"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Full Name"
              fullWidth
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <Controller
          name="personalInfo.email"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Email"
              type="email"
              fullWidth
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <Controller
          name="personalInfo.phone"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Phone"
              fullWidth
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <Controller
          name="personalInfo.location"
          control={control}
          render={({ field, fieldState }) => (
            <TextField
              {...field}
              label="Location"
              fullWidth
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      </Grid>
      <Grid size={{ xs: 12 }}>
        <Controller
          name="personalInfo.headline"
          control={control}
          render={({ field }) => (
            <TextField
              {...field}
              label="Headline / Job Title"
              placeholder="e.g. Head of Technology"
              fullWidth
            />
          )}
        />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <Controller
          name="personalInfo.linkedIn"
          control={control}
          render={({ field }) => (
            <TextField {...field} label="LinkedIn URL" fullWidth />
          )}
        />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <Controller
          name="personalInfo.website"
          control={control}
          render={({ field }) => (
            <TextField {...field} label="Website / Portfolio" fullWidth />
          )}
        />
      </Grid>
    </Grid>
  );
}
