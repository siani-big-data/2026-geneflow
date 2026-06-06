"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Plus,
  Trash2,
  FileText,
  ExternalLink,
  Book,
  Calendar,
  Users,
  AlertCircle,
  Loader2,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  useStudyPapers,
  useAddStudyPaper,
  useRemoveStudyPaper,
} from "@/hooks/use-studies";
import type { StudyPaper, AddStudyPaperInput } from "@/types/study";

interface PapersTabProps {
  studyId: string;
  canEdit?: boolean;
}

const initialFormState: AddStudyPaperInput = {
  title: "",
  authors: "",
  doi: "",
  abstract: "",
  journal: "",
  publicationYear: undefined,
};

export function PapersTab({ studyId, canEdit = false }: PapersTabProps) {
  const t = useTranslations("studies.papers");
  const tCommon = useTranslations("common");

  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [paperToDelete, setPaperToDelete] = useState<StudyPaper | null>(null);
  const [formData, setFormData] = useState<AddStudyPaperInput>(initialFormState);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const { data: papers, isLoading, error } = useStudyPapers(studyId);
  const addPaper = useAddStudyPaper(studyId);
  const removePaper = useRemoveStudyPaper(studyId);

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.title?.trim()) {
      errors.title = t("validation.titleRequired");
    } else if (formData.title.length > 500) {
      errors.title = t("validation.titleTooLong");
    }

    if (formData.authors && formData.authors.length > 1000) {
      errors.authors = t("validation.authorsTooLong");
    }

    if (formData.doi && formData.doi.length > 100) {
      errors.doi = t("validation.doiTooLong");
    }

    if (formData.abstract && formData.abstract.length > 5000) {
      errors.abstract = t("validation.abstractTooLong");
    }

    if (formData.journal && formData.journal.length > 200) {
      errors.journal = t("validation.journalTooLong");
    }

    if (formData.publicationYear) {
      const year = formData.publicationYear;
      const currentYear = new Date().getFullYear();
      if (year < 1900 || year > currentYear + 1) {
        errors.publicationYear = t("validation.invalidYear");
      }
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleAddPaper = async () => {
    if (!validateForm()) return;

    try {
      await addPaper.mutateAsync({
        title: formData.title.trim(),
        authors: formData.authors?.trim() || undefined,
        doi: formData.doi?.trim() || undefined,
        abstract: formData.abstract?.trim() || undefined,
        journal: formData.journal?.trim() || undefined,
        publicationYear: formData.publicationYear || undefined,
      });
      setIsAddDialogOpen(false);
      setFormData(initialFormState);
      setFormErrors({});
    } catch (err) {
      console.error("Failed to add paper:", err);
    }
  };

  const handleDeletePaper = async () => {
    if (!paperToDelete) return;

    try {
      await removePaper.mutateAsync(paperToDelete.id);
      setPaperToDelete(null);
      setIsDeleteDialogOpen(false);
    } catch (err) {
      console.error("Failed to remove paper:", err);
    }
  };

  const openDeleteDialog = (paper: StudyPaper) => {
    setPaperToDelete(paper);
    setIsDeleteDialogOpen(true);
  };

  const openDoiLink = (doi: string) => {
    window.open(`https://doi.org/${doi}`, "_blank", "noopener,noreferrer");
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <AlertCircle className="h-12 w-12 text-destructive mb-4" />
        <p className="text-muted-foreground">{t("loadError")}</p>
      </div>
    );
  }

  const papersList = papers ?? [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold">{t("title")}</h3>
          <p className="text-sm text-muted-foreground">
            {t("description", { count: papersList.length })}
          </p>
        </div>
        {canEdit && (
          <Button onClick={() => setIsAddDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            {t("addPaper")}
          </Button>
        )}
      </div>

      {/* Papers List */}
      {papersList.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Book className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="text-muted-foreground text-center">{t("noPapers")}</p>
            {canEdit && (
              <Button
                variant="outline"
                className="mt-4"
                onClick={() => setIsAddDialogOpen(true)}
              >
                <Plus className="h-4 w-4 mr-2" />
                {t("addFirstPaper")}
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {papersList.map((paper) => (
            <PaperCard
              key={paper.id}
              paper={paper}
              canEdit={canEdit}
              onDelete={() => openDeleteDialog(paper)}
              onOpenDoi={() => paper.doi && openDoiLink(paper.doi)}
            />
          ))}
        </div>
      )}

      {/* Add Paper Dialog */}
      <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{t("addPaperDialog.title")}</DialogTitle>
            <DialogDescription>
              {t("addPaperDialog.description")}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            {/* Title */}
            <div className="space-y-2">
              <label htmlFor="title" className="text-sm font-medium">
                {t("form.title")} <span className="text-destructive">*</span>
              </label>
              <Input
                id="title"
                value={formData.title}
                onChange={(e) =>
                  setFormData({ ...formData, title: e.target.value })
                }
                placeholder={t("form.titlePlaceholder")}
                className={formErrors.title ? "border-destructive" : ""}
              />
              {formErrors.title && (
                <p className="text-sm text-destructive">{formErrors.title}</p>
              )}
            </div>

            {/* Authors */}
            <div className="space-y-2">
              <label htmlFor="authors" className="text-sm font-medium">
                {t("form.authors")}
              </label>
              <Input
                id="authors"
                value={formData.authors ?? ""}
                onChange={(e) =>
                  setFormData({ ...formData, authors: e.target.value })
                }
                placeholder={t("form.authorsPlaceholder")}
                className={formErrors.authors ? "border-destructive" : ""}
              />
              {formErrors.authors && (
                <p className="text-sm text-destructive">{formErrors.authors}</p>
              )}
            </div>

            {/* DOI and Year Row */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="doi" className="text-sm font-medium">
                  {t("form.doi")}
                </label>
                <Input
                  id="doi"
                  value={formData.doi ?? ""}
                  onChange={(e) =>
                    setFormData({ ...formData, doi: e.target.value })
                  }
                  placeholder={t("form.doiPlaceholder")}
                  className={formErrors.doi ? "border-destructive" : ""}
                />
                {formErrors.doi && (
                  <p className="text-sm text-destructive">{formErrors.doi}</p>
                )}
              </div>

              <div className="space-y-2">
                <label htmlFor="publicationYear" className="text-sm font-medium">
                  {t("form.year")}
                </label>
                <Input
                  id="publicationYear"
                  type="number"
                  value={formData.publicationYear ?? ""}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      publicationYear: e.target.value
                        ? parseInt(e.target.value)
                        : undefined,
                    })
                  }
                  placeholder={t("form.yearPlaceholder")}
                  className={formErrors.publicationYear ? "border-destructive" : ""}
                />
                {formErrors.publicationYear && (
                  <p className="text-sm text-destructive">
                    {formErrors.publicationYear}
                  </p>
                )}
              </div>
            </div>

            {/* Journal */}
            <div className="space-y-2">
              <label htmlFor="journal" className="text-sm font-medium">
                {t("form.journal")}
              </label>
              <Input
                id="journal"
                value={formData.journal ?? ""}
                onChange={(e) =>
                  setFormData({ ...formData, journal: e.target.value })
                }
                placeholder={t("form.journalPlaceholder")}
                className={formErrors.journal ? "border-destructive" : ""}
              />
              {formErrors.journal && (
                <p className="text-sm text-destructive">{formErrors.journal}</p>
              )}
            </div>

            {/* Abstract */}
            <div className="space-y-2">
              <label htmlFor="abstract" className="text-sm font-medium">
                {t("form.abstract")}
              </label>
              <textarea
                id="abstract"
                value={formData.abstract ?? ""}
                onChange={(e) =>
                  setFormData({ ...formData, abstract: e.target.value })
                }
                placeholder={t("form.abstractPlaceholder")}
                rows={4}
                className={`w-full rounded-md border bg-background px-3 py-2 text-sm ${
                  formErrors.abstract ? "border-destructive" : "border-input"
                }`}
              />
              {formErrors.abstract && (
                <p className="text-sm text-destructive">{formErrors.abstract}</p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setIsAddDialogOpen(false);
                setFormData(initialFormState);
                setFormErrors({});
              }}
            >
              {tCommon("cancel")}
            </Button>
            <Button onClick={handleAddPaper} disabled={addPaper.isPending}>
              {addPaper.isPending && (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              )}
              {t("addPaperDialog.submit")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("deleteDialog.title")}</DialogTitle>
            <DialogDescription>
              {t("deleteDialog.description", { title: paperToDelete?.title ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setIsDeleteDialogOpen(false);
                setPaperToDelete(null);
              }}
            >
              {tCommon("cancel")}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeletePaper}
              disabled={removePaper.isPending}
            >
              {removePaper.isPending && (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              )}
              {tCommon("delete")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// Paper Card Component
interface PaperCardProps {
  paper: StudyPaper;
  canEdit: boolean;
  onDelete: () => void;
  onOpenDoi: () => void;
}

function PaperCard({ paper, canEdit, onDelete, onOpenDoi }: PaperCardProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-base font-medium leading-tight">
              {paper.title}
            </CardTitle>
            {paper.authors && (
              <CardDescription className="mt-1 flex items-center gap-1">
                <Users className="h-3 w-3 flex-shrink-0" />
                <span className="truncate">{paper.authors}</span>
              </CardDescription>
            )}
          </div>
          <div className="flex items-center gap-2 flex-shrink-0">
            {paper.doi && (
              <Button
                variant="outline"
                size="sm"
                onClick={onOpenDoi}
                className="gap-1"
              >
                <ExternalLink className="h-3 w-3" />
                DOI
              </Button>
            )}
            {canEdit && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onDelete}
                className="text-destructive hover:text-destructive hover:bg-destructive/10"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        {/* Metadata Row */}
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          {paper.journal && (
            <span className="flex items-center gap-1">
              <Book className="h-3 w-3" />
              {paper.journal}
            </span>
          )}
          {paper.publicationYear && (
            <span className="flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {paper.publicationYear}
            </span>
          )}
          {paper.doi && (
            <span className="flex items-center gap-1">
              <FileText className="h-3 w-3" />
              {paper.doi}
            </span>
          )}
        </div>

        {/* Abstract */}
        {paper.abstract && (
          <p className="mt-3 text-sm text-muted-foreground line-clamp-3">
            {paper.abstract}
          </p>
        )}
      </CardContent>
    </Card>
  );
}
