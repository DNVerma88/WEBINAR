import { Box, Button, Card, CardContent, Grid, IconButton, TextField, Typography } from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { Controller, useFieldArray } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function AchievementsSection({ control }: Props) {
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'achievements',
    keyName: 'rhfId',
  });

  return (
    <Box>
      {fields.map((field, i) => (
        <Card key={field.rhfId} sx={{ mb: 2 }} variant="outlined">
          <CardContent>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="subtitle2">Achievement #{i + 1}</Typography>
              <IconButton size="small" color="error" onClick={() => remove(i)}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 8 }}>
                <Controller
                  name={`achievements.${i}.title`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Achievement Title" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Controller
                  name={`achievements.${i}.year`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField {...f} label="Year" placeholder="e.g. 2023" fullWidth />
                  )}
                />
              </Grid>
              <Grid size={{ xs: 12 }}>
                <Controller
                  name={`achievements.${i}.description`}
                  control={control}
                  render={({ field: f }) => (
                    <TextField
                      {...f}
                      label="Description"
                      multiline
                      rows={2}
                      fullWidth
                      placeholder="Briefly describe this achievement..."
                    />
                  )}
                />
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      ))}
      <Button
        variant="outlined"
        startIcon={<AddIcon />}
        onClick={() => append({ id: crypto.randomUUID(), title: '', year: '', description: '' })}
      >
        Add Achievement
      </Button>
    </Box>
  );
}
