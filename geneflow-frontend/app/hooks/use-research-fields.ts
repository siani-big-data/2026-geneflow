"use client";

import { useMemo } from "react";
import { useTranslations } from "next-intl";
import { RESEARCH_FIELDS } from "@/types/profile";

export interface TranslatedResearchField {
  id: number;
  name: string;
  label: string;
}

/**
 * Hook that provides research fields with translated labels.
 * Uses the current locale to translate field names.
 */
export function useTranslatedResearchFields() {
  const t = useTranslations("common.researchFields");

  const translatedFields = useMemo<TranslatedResearchField[]>(() => {
    return RESEARCH_FIELDS.map((field) => ({
      id: field.id,
      name: field.name,
      label: t(field.name),
    }));
  }, [t]);

  /**
   * Get the translated label for a research field name.
   */
  const getFieldLabel = (name: string | null | undefined): string => {
    if (!name) return "";
    return t(name);
  };

  /**
   * Get a field by its ID with translated label.
   */
  const getFieldById = (id: number): TranslatedResearchField | undefined => {
    return translatedFields.find((f) => f.id === id);
  };

  /**
   * Get a field by its name with translated label.
   */
  const getFieldByName = (name: string): TranslatedResearchField | undefined => {
    return translatedFields.find((f) => f.name === name);
  };

  return {
    fields: translatedFields,
    getFieldLabel,
    getFieldById,
    getFieldByName,
  };
}
