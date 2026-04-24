import { Box, Button, Chip, Stack, TextField, Typography } from '@/components/ui';
import { AddIcon, DeleteIcon } from '@/components/ui';
import { useState } from 'react';
import { useFieldArray, useWatch } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import type { ResumeProfile } from '../../types';

interface Props {
  control: Control<ResumeProfile>;
}

export function SkillsSection({ control }: Props) {
  const [newSkill, setNewSkill] = useState('');
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'skills',
    keyName: 'rhfId',
  });

  const skills = useWatch({ control, name: 'skills' });

  const handleAdd = () => {
    const trimmed = newSkill.trim();
    if (!trimmed) return;
    append({ id: crypto.randomUUID(), name: trimmed, level: '' });
    setNewSkill('');
  };

  return (
    <Box>
      <Box display="flex" gap={1} mb={2}>
        <TextField
          label="Add Skill"
          value={newSkill}
          onChange={(e) => setNewSkill(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              handleAdd();
            }
          }}
          size="small"
          sx={{ flex: 1 }}
        />
        <Button startIcon={<AddIcon />} onClick={handleAdd} variant="outlined">
          Add
        </Button>
      </Box>
      {fields.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          No skills added yet. Type a skill name and press Add or Enter.
        </Typography>
      ) : (
        <Stack direction="row" flexWrap="wrap" gap={1}>
          {fields.map((field, i) => (
            <Chip
              key={field.rhfId}
              label={skills?.[i]?.name ?? field.name}
              onDelete={() => remove(i)}
              deleteIcon={<DeleteIcon />}
            />
          ))}
        </Stack>
      )}
    </Box>
  );
}
