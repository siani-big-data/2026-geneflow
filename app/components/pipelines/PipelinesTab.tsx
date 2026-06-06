"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Plus,
  Play,
  Pause,
  Trash2,
  Edit,
  Archive,
  Loader2,
  Workflow,
  CheckCircle2,
  XCircle,
  Clock,
  AlertCircle,
  ChevronDown,
  ChevronUp,
  MoreHorizontal,
  Zap,
  ArrowRight,
  Beaker,
  Scissors,
  Dna,
  Search,
  FlaskConical,
  Microscope,
  X,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  usePipelines,
  usePipeline,
  useStepTypes,
  useCreatePipeline,
  useUpdatePipeline,
  useDeletePipeline,
  useActivatePipeline,
  useDeactivatePipeline,
  useArchivePipeline,
  useAddPipelineStep,
  useRemovePipelineStep,
  useExecutePipeline,
  usePipelineExecutions,
} from "@/hooks/use-pipelines";
import type { StepType } from "@/types/pipeline";

interface PipelinesTabProps {
  studyId: string;
  traces: Array<{ id: string; name: string; status: string }>;
}

// Step type icons mapping
const stepTypeIcons: Record<string, React.ComponentType<{ className?: string }>> = {
  Quality: Beaker,
  Trimming: Scissors,
  Heterozygote: Dna,
  Motif: Search,
  Translation: FlaskConical,
  ORF: Microscope,
  Restriction: Zap,
};

function getStepIcon(stepTypeName: string) {
  const Icon = stepTypeIcons[stepTypeName] || Workflow;
  return Icon;
}

export function PipelinesTab({ studyId, traces }: PipelinesTabProps) {
  const t = useTranslations("pipelines");
  const tCommon = useTranslations("common");

  // UI State
  const [expandedPipelineId, setExpandedPipelineId] = useState<string | null>(null);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [addStepDialogOpen, setAddStepDialogOpen] = useState(false);
  const [executeDialogOpen, setExecuteDialogOpen] = useState(false);
  const [selectedPipelineId, setSelectedPipelineId] = useState<string | null>(null);

  // Form state
  const [pipelineForm, setPipelineForm] = useState({ name: "", description: "" });
  const [selectedStepType, setSelectedStepType] = useState<number | null>(null);
  const [selectedTraceId, setSelectedTraceId] = useState<string>("");
  const [stepConfig, setStepConfig] = useState<Record<string, unknown>>({});

  // Queries
  const { data: pipelinesData, isLoading } = usePipelines(studyId);
  const { data: expandedPipeline } = usePipeline(studyId, expandedPipelineId ?? "");
  const { data: stepTypes } = useStepTypes();
  const { data: executionsData } = usePipelineExecutions(studyId, expandedPipelineId ?? "", undefined, 1, 3);

  // Mutations
  const createPipeline = useCreatePipeline(studyId);
  const updatePipeline = useUpdatePipeline(studyId, selectedPipelineId ?? "");
  const deletePipeline = useDeletePipeline(studyId);
  const activatePipeline = useActivatePipeline(studyId, selectedPipelineId ?? "");
  const deactivatePipeline = useDeactivatePipeline(studyId, selectedPipelineId ?? "");
  const archivePipeline = useArchivePipeline(studyId, selectedPipelineId ?? "");
  const addStep = useAddPipelineStep(studyId, expandedPipelineId ?? "");
  const removeStep = useRemovePipelineStep(studyId, expandedPipelineId ?? "");
  const executePipeline = useExecutePipeline(studyId, selectedPipelineId ?? "");

  const pipelines = pipelinesData?.items ?? [];
  const processedTraces = traces.filter((t) => t.status === "processed" || t.status === "Processed");

  // Handlers
  const handleCreatePipeline = async () => {
    try {
      const result = await createPipeline.mutateAsync(pipelineForm);
      setCreateDialogOpen(false);
      setPipelineForm({ name: "", description: "" });
      setExpandedPipelineId(result.id);
    } catch (err) {
      console.error("Failed to create pipeline:", err);
    }
  };

  const handleUpdatePipeline = async () => {
    try {
      await updatePipeline.mutateAsync(pipelineForm);
      setEditDialogOpen(false);
    } catch (err) {
      console.error("Failed to update pipeline:", err);
    }
  };

  const handleDeletePipeline = async () => {
    if (!selectedPipelineId) return;
    try {
      await deletePipeline.mutateAsync(selectedPipelineId);
      setDeleteDialogOpen(false);
      setSelectedPipelineId(null);
      if (expandedPipelineId === selectedPipelineId) {
        setExpandedPipelineId(null);
      }
    } catch (err) {
      console.error("Failed to delete pipeline:", err);
    }
  };

  const handleAddStep = async () => {
    if (!selectedStepType) return;
    try {
      const config = Object.keys(stepConfig).length > 0 ? JSON.stringify(stepConfig) : undefined;
      await addStep.mutateAsync({ stepTypeId: selectedStepType, configuration: config });
      setAddStepDialogOpen(false);
      setSelectedStepType(null);
      setStepConfig({});
    } catch (err) {
      console.error("Failed to add step:", err);
    }
  };

  const handleRemoveStep = async (stepId: string) => {
    try {
      await removeStep.mutateAsync(stepId);
    } catch (err) {
      console.error("Failed to remove step:", err);
    }
  };

  const handleExecutePipeline = async () => {
    if (!selectedTraceId) return;
    try {
      await executePipeline.mutateAsync({ traceId: selectedTraceId });
      setExecuteDialogOpen(false);
      setSelectedTraceId("");
    } catch (err) {
      console.error("Failed to execute pipeline:", err);
    }
  };

  const handleActivate = async (pipelineId: string) => {
    setSelectedPipelineId(pipelineId);
    try {
      await activatePipeline.mutateAsync();
    } catch (err) {
      console.error("Failed to activate pipeline:", err);
    }
  };

  const handleDeactivate = async (pipelineId: string) => {
    setSelectedPipelineId(pipelineId);
    try {
      await deactivatePipeline.mutateAsync();
    } catch (err) {
      console.error("Failed to deactivate pipeline:", err);
    }
  };

  const openEditDialog = (pipeline: { id: string; name: string; description?: string | null }) => {
    setSelectedPipelineId(pipeline.id);
    setPipelineForm({
      name: pipeline.name,
      description: pipeline.description ?? "",
    });
    setEditDialogOpen(true);
  };

  const openDeleteDialog = (pipelineId: string) => {
    setSelectedPipelineId(pipelineId);
    setDeleteDialogOpen(true);
  };

  const openExecuteDialog = (pipelineId: string) => {
    setSelectedPipelineId(pipelineId);
    setExecuteDialogOpen(true);
  };

  const toggleExpanded = (pipelineId: string) => {
    setExpandedPipelineId(expandedPipelineId === pipelineId ? null : pipelineId);
  };

  const getStatusConfig = (status: string) => {
    switch (status) {
      case "Active":
        return { color: "bg-emerald-500/10 text-emerald-600 border-emerald-500/20", icon: CheckCircle2 };
      case "Draft":
        return { color: "bg-amber-500/10 text-amber-600 border-amber-500/20", icon: Clock };
      case "Archived":
        return { color: "bg-muted text-muted-foreground border-border", icon: Archive };
      default:
        return { color: "bg-muted text-muted-foreground border-border", icon: AlertCircle };
    }
  };

  const getExecutionStatusIcon = (status: string) => {
    switch (status) {
      case "Completed":
        return <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />;
      case "Failed":
        return <XCircle className="h-3.5 w-3.5 text-red-500" />;
      case "Running":
        return <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-500" />;
      case "Pending":
        return <Clock className="h-3.5 w-3.5 text-amber-500" />;
      default:
        return <AlertCircle className="h-3.5 w-3.5 text-muted-foreground" />;
    }
  };

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-foreground">{t("title")}</h2>
          <p className="text-sm text-muted-foreground">{t("description")}</p>
        </div>
        <button
          onClick={() => setCreateDialogOpen(true)}
          className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-teal/90"
        >
          <Plus className="h-4 w-4" />
          {t("newPipeline")}
        </button>
      </div>

      {/* Empty State */}
      {pipelines.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-border bg-muted/20 py-16">
          <div className="mb-4 rounded-full bg-teal/10 p-4">
            <Workflow className="h-8 w-8 text-teal" />
          </div>
          <h3 className="mb-1 text-lg font-medium text-foreground">{t("noPipelines")}</h3>
          <p className="mb-6 max-w-sm text-center text-sm text-muted-foreground">
            {t("noPipelinesDesc")}
          </p>
          <button
            onClick={() => setCreateDialogOpen(true)}
            className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90"
          >
            <Plus className="h-4 w-4" />
            {t("createFirst")}
          </button>
        </div>
      ) : (
        /* Pipeline List */
        <div className="space-y-3">
          {pipelines.map((pipeline) => {
            const isExpanded = expandedPipelineId === pipeline.id;
            const statusConfig = getStatusConfig(pipeline.statusName);
            const StatusIcon = statusConfig.icon;

            return (
              <div
                key={pipeline.id}
                className={cn(
                  "rounded-xl border bg-card transition-all",
                  isExpanded ? "border-teal shadow-sm" : "border-border hover:border-muted-foreground"
                )}
              >
                {/* Pipeline Header */}
                <div
                  className="flex cursor-pointer items-center gap-4 p-4"
                  onClick={() => toggleExpanded(pipeline.id)}
                >
                  {/* Icon */}
                  <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg bg-teal/10">
                    <Workflow className="h-5 w-5 text-teal" />
                  </div>

                  {/* Info */}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium text-foreground">{pipeline.name}</h3>
                      <span className={cn("flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium", statusConfig.color)}>
                        <StatusIcon className="h-3 w-3" />
                        {pipeline.statusName}
                      </span>
                    </div>
                    <p className="mt-0.5 text-sm text-muted-foreground line-clamp-1">
                      {pipeline.description || t("noDescription")}
                    </p>
                  </div>

                  {/* Steps count */}
                  <div className="hidden items-center gap-4 text-sm text-muted-foreground sm:flex">
                    <div className="flex items-center gap-1.5">
                      <div className="flex -space-x-1">
                        {Array.from({ length: Math.min(pipeline.stepCount, 4) }).map((_, i) => (
                          <div
                            key={i}
                            className="flex h-5 w-5 items-center justify-center rounded-full border-2 border-card bg-muted text-[10px] font-medium"
                          >
                            {i + 1}
                          </div>
                        ))}
                        {pipeline.stepCount > 4 && (
                          <div className="flex h-5 w-5 items-center justify-center rounded-full border-2 border-card bg-teal text-[10px] font-medium text-white">
                            +{pipeline.stepCount - 4}
                          </div>
                        )}
                      </div>
                      <span>{pipeline.stepCount} {t("steps")}</span>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-1">
                    {pipeline.statusName === "Active" && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          openExecuteDialog(pipeline.id);
                        }}
                        disabled={processedTraces.length === 0}
                        className="flex items-center gap-1.5 rounded-lg bg-teal px-3 py-1.5 text-xs font-medium text-white hover:bg-teal/90 disabled:opacity-50"
                      >
                        <Play className="h-3.5 w-3.5" />
                        {t("execute.run")}
                      </button>
                    )}
                    <button
                      onClick={(e) => e.stopPropagation()}
                      className="rounded-lg p-2 text-muted-foreground hover:bg-muted hover:text-foreground"
                    >
                      {isExpanded ? (
                        <ChevronUp className="h-4 w-4" />
                      ) : (
                        <ChevronDown className="h-4 w-4" />
                      )}
                    </button>
                  </div>
                </div>

                {/* Expanded Content */}
                {isExpanded && expandedPipeline && (
                  <div className="border-t border-border">
                    {/* Action Bar */}
                    <div className="flex flex-wrap items-center gap-2 border-b border-border bg-muted/30 px-4 py-3">
                      {expandedPipeline.statusName === "Draft" && (
                        <>
                          {expandedPipeline.stepCount > 0 && (
                            <button
                              onClick={() => handleActivate(expandedPipeline.id)}
                              disabled={activatePipeline.isPending}
                              className="flex items-center gap-1.5 rounded-lg bg-emerald-500 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-500/90 disabled:opacity-50"
                            >
                              {activatePipeline.isPending ? (
                                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                              ) : (
                                <Play className="h-3.5 w-3.5" />
                              )}
                              {t("activate")}
                            </button>
                          )}
                          <button
                            onClick={() => openEditDialog(expandedPipeline)}
                            className="flex items-center gap-1.5 rounded-lg border border-border bg-background px-3 py-1.5 text-xs font-medium text-foreground hover:bg-muted"
                          >
                            <Edit className="h-3.5 w-3.5" />
                            {tCommon("edit")}
                          </button>
                        </>
                      )}
                      {expandedPipeline.statusName === "Active" && (
                        <>
                          <button
                            onClick={() => handleDeactivate(expandedPipeline.id)}
                            disabled={deactivatePipeline.isPending}
                            className="flex items-center gap-1.5 rounded-lg border border-border bg-background px-3 py-1.5 text-xs font-medium text-foreground hover:bg-muted disabled:opacity-50"
                          >
                            {deactivatePipeline.isPending ? (
                              <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            ) : (
                              <Pause className="h-3.5 w-3.5" />
                            )}
                            {t("deactivate")}
                          </button>
                          <button
                            onClick={() => archivePipeline.mutate()}
                            disabled={archivePipeline.isPending}
                            className="flex items-center gap-1.5 rounded-lg border border-border bg-background px-3 py-1.5 text-xs font-medium text-muted-foreground hover:bg-muted disabled:opacity-50"
                          >
                            {archivePipeline.isPending ? (
                              <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            ) : (
                              <Archive className="h-3.5 w-3.5" />
                            )}
                            {t("archive")}
                          </button>
                        </>
                      )}
                      <div className="flex-1" />
                      <button
                        onClick={() => openDeleteDialog(expandedPipeline.id)}
                        className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium text-red-500 hover:bg-red-500/10"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                        {tCommon("delete")}
                      </button>
                    </div>

                    {/* Steps Section */}
                    <div className="p-4">
                      <div className="mb-3 flex items-center justify-between">
                        <h4 className="text-sm font-medium text-foreground">{t("pipelineSteps")}</h4>
                        {expandedPipeline.canBeEdited && (
                          <button
                            onClick={() => setAddStepDialogOpen(true)}
                            className="flex items-center gap-1 rounded-md bg-teal/10 px-2.5 py-1 text-xs font-medium text-teal hover:bg-teal/20"
                          >
                            <Plus className="h-3 w-3" />
                            {t("addStepLabel")}
                          </button>
                        )}
                      </div>

                      {expandedPipeline.steps.length === 0 ? (
                        <div className="rounded-lg border border-dashed border-border bg-muted/20 py-8 text-center">
                          <p className="text-sm text-muted-foreground">{t("noSteps")}</p>
                          {expandedPipeline.canBeEdited && (
                            <button
                              onClick={() => setAddStepDialogOpen(true)}
                              className="mt-2 text-sm font-medium text-teal hover:underline"
                            >
                              {t("addFirstStep")}
                            </button>
                          )}
                        </div>
                      ) : (
                        <div className="flex flex-wrap items-center gap-2">
                          {expandedPipeline.steps.map((step, index) => {
                            const StepIcon = getStepIcon(step.stepTypeName);
                            return (
                              <div key={step.id} className="flex items-center gap-2">
                                <div
                                  className={cn(
                                    "group relative flex items-center gap-2 rounded-lg border bg-background px-3 py-2",
                                    step.isEnabled ? "border-border" : "border-dashed border-muted opacity-50"
                                  )}
                                >
                                  <div className="flex h-6 w-6 items-center justify-center rounded-md bg-teal/10">
                                    <StepIcon className="h-3.5 w-3.5 text-teal" />
                                  </div>
                                  <div>
                                    <p className="text-sm font-medium text-foreground">
                                      {step.displayName || step.stepTypeDisplayName}
                                    </p>
                                    {step.label && (
                                      <p className="text-xs text-muted-foreground">{step.label}</p>
                                    )}
                                  </div>
                                  {expandedPipeline.canBeEdited && (
                                    <button
                                      onClick={() => handleRemoveStep(step.id)}
                                      className="ml-1 rounded p-0.5 text-muted-foreground opacity-0 transition-opacity hover:bg-red-500/10 hover:text-red-500 group-hover:opacity-100"
                                    >
                                      <X className="h-3.5 w-3.5" />
                                    </button>
                                  )}
                                </div>
                                {index < expandedPipeline.steps.length - 1 && (
                                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                                )}
                              </div>
                            );
                          })}
                        </div>
                      )}
                    </div>

                    {/* Recent Executions */}
                    {executionsData && executionsData.items.length > 0 && (
                      <div className="border-t border-border px-4 py-3">
                        <h4 className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                          {t("recentExecutions")}
                        </h4>
                        <div className="space-y-1.5">
                          {executionsData.items.map((exec) => (
                            <div
                              key={exec.id}
                              className="flex items-center gap-3 rounded-lg bg-muted/30 px-3 py-2"
                            >
                              {getExecutionStatusIcon(exec.statusName)}
                              <span className="flex-1 text-sm text-foreground">
                                {traces.find((t) => t.id === exec.traceId)?.name || exec.traceId}
                              </span>
                              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                                <span>
                                  {exec.completedSteps}/{exec.totalSteps} {t("steps")}
                                </span>
                                <span>•</span>
                                <span>{new Date(exec.createdAt).toLocaleDateString()}</span>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Create Pipeline Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("create.title")}</DialogTitle>
            <DialogDescription>{t("create.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("create.name")}</label>
              <input
                type="text"
                value={pipelineForm.name}
                onChange={(e) => setPipelineForm((f) => ({ ...f, name: e.target.value }))}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                placeholder={t("create.namePlaceholder")}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("create.descriptionLabel")}</label>
              <textarea
                rows={3}
                value={pipelineForm.description}
                onChange={(e) => setPipelineForm((f) => ({ ...f, description: e.target.value }))}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                placeholder={t("create.descriptionPlaceholder")}
              />
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setCreateDialogOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleCreatePipeline}
              disabled={!pipelineForm.name.trim() || createPipeline.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {createPipeline.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : t("create.submit")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Pipeline Dialog */}
      <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("edit.title")}</DialogTitle>
            <DialogDescription>{t("edit.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("create.name")}</label>
              <input
                type="text"
                value={pipelineForm.name}
                onChange={(e) => setPipelineForm((f) => ({ ...f, name: e.target.value }))}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("create.descriptionLabel")}</label>
              <textarea
                rows={3}
                value={pipelineForm.description}
                onChange={(e) => setPipelineForm((f) => ({ ...f, description: e.target.value }))}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setEditDialogOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleUpdatePipeline}
              disabled={!pipelineForm.name.trim() || updatePipeline.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {updatePipeline.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : t("edit.save")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Pipeline Dialog */}
      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle className="text-red-500">{t("delete.title")}</DialogTitle>
            <DialogDescription>{t("delete.description")}</DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4">
            <button onClick={() => setDeleteDialogOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleDeletePipeline}
              disabled={deletePipeline.isPending}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90 disabled:opacity-50"
            >
              {deletePipeline.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : t("delete.confirm")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Step Dialog */}
      <Dialog open={addStepDialogOpen} onOpenChange={(open) => {
        setAddStepDialogOpen(open);
        if (!open) {
          setSelectedStepType(null);
          setStepConfig({});
        }
      }}>
        <DialogContent className="sm:max-w-[520px]">
          <DialogHeader>
            <DialogTitle>{t("addStep.title")}</DialogTitle>
            <DialogDescription>{t("addStep.description")}</DialogDescription>
          </DialogHeader>
          <div className="py-4 space-y-4">
            <div className="grid gap-2 sm:grid-cols-2">
              {stepTypes?.map((stepType) => {
                const StepIcon = getStepIcon(stepType.name);
                return (
                  <button
                    key={stepType.id}
                    onClick={() => {
                      setSelectedStepType(stepType.id);
                      // Set default config based on step type
                      const defaults: Record<string, Record<string, unknown>> = {
                        Trimming: { algorithm: "mott", cutoff: 0.05 },
                        Heterozygote: { min_ratio: 0.3, max_ratio: 0.7 },
                        Motif: { pattern: "", search_complement: false },
                        Translation: { frame: 1 },
                        ORF: { min_length: 100 },
                        Restriction: { enzymes: [] },
                      };
                      setStepConfig(defaults[stepType.name] ?? {});
                    }}
                    className={cn(
                      "flex items-center gap-3 rounded-lg border p-3 text-left transition-all",
                      selectedStepType === stepType.id
                        ? "border-teal bg-teal/5 ring-1 ring-teal"
                        : "border-border hover:border-teal/50 hover:bg-muted/50"
                    )}
                  >
                    <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-teal/10">
                      <StepIcon className="h-4 w-4 text-teal" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="font-medium text-foreground">{stepType.displayName}</p>
                      <p className="truncate text-xs text-muted-foreground">{stepType.name}</p>
                    </div>
                  </button>
                );
              })}
            </div>

            {/* Configuration forms based on selected step type */}
            {selectedStepType && stepTypes && (() => {
              const selectedType = stepTypes.find(s => s.id === selectedStepType);
              if (!selectedType?.requiresConfiguration) return null;

              return (
                <div className="rounded-lg border border-border bg-muted/30 p-4 space-y-3">
                  <p className="text-sm font-medium text-foreground">{t("stepConfig.title")}</p>

                  {selectedType.name === "Trimming" && (
                    <>
                      <div className="space-y-1.5">
                        <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.algorithm")}</label>
                        <select
                          value={(stepConfig.algorithm as string) ?? "mott"}
                          onChange={(e) => setStepConfig(c => ({ ...c, algorithm: e.target.value }))}
                          className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        >
                          <option value="mott">Mott</option>
                          <option value="lucy">Lucy</option>
                        </select>
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.cutoff")}</label>
                        <input
                          type="number"
                          step="0.01"
                          min="0.01"
                          max="0.5"
                          value={(stepConfig.cutoff as number) ?? 0.05}
                          onChange={(e) => setStepConfig(c => ({ ...c, cutoff: parseFloat(e.target.value) }))}
                          className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        />
                      </div>
                    </>
                  )}

                  {selectedType.name === "Heterozygote" && (
                    <>
                      <div className="space-y-1.5">
                        <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.minRatio")}</label>
                        <input
                          type="number"
                          step="0.1"
                          min="0.1"
                          max="0.5"
                          value={(stepConfig.min_ratio as number) ?? 0.3}
                          onChange={(e) => setStepConfig(c => ({ ...c, min_ratio: parseFloat(e.target.value) }))}
                          className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.maxRatio")}</label>
                        <input
                          type="number"
                          step="0.1"
                          min="0.5"
                          max="0.9"
                          value={(stepConfig.max_ratio as number) ?? 0.7}
                          onChange={(e) => setStepConfig(c => ({ ...c, max_ratio: parseFloat(e.target.value) }))}
                          className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        />
                      </div>
                    </>
                  )}

                  {selectedType.name === "Motif" && (
                    <>
                      <div className="space-y-1.5">
                        <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.pattern")} *</label>
                        <input
                          type="text"
                          value={(stepConfig.pattern as string) ?? ""}
                          onChange={(e) => setStepConfig(c => ({ ...c, pattern: e.target.value }))}
                          placeholder="e.g., GAATTC"
                          className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                        />
                      </div>
                      <label className="flex items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          checked={(stepConfig.search_complement as boolean) ?? false}
                          onChange={(e) => setStepConfig(c => ({ ...c, search_complement: e.target.checked }))}
                          className="rounded border-border"
                        />
                        {t("stepConfig.searchComplement")}
                      </label>
                    </>
                  )}

                  {selectedType.name === "Translation" && (
                    <div className="space-y-1.5">
                      <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.frame")}</label>
                      <select
                        value={(stepConfig.frame as number) ?? 1}
                        onChange={(e) => setStepConfig(c => ({ ...c, frame: parseInt(e.target.value) }))}
                        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                      >
                        <option value={1}>Frame +1</option>
                        <option value={2}>Frame +2</option>
                        <option value={3}>Frame +3</option>
                        <option value={-1}>Frame -1</option>
                        <option value={-2}>Frame -2</option>
                        <option value={-3}>Frame -3</option>
                      </select>
                    </div>
                  )}

                  {selectedType.name === "ORF" && (
                    <div className="space-y-1.5">
                      <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.minLength")}</label>
                      <input
                        type="number"
                        min="30"
                        value={(stepConfig.min_length as number) ?? 100}
                        onChange={(e) => setStepConfig(c => ({ ...c, min_length: parseInt(e.target.value) }))}
                        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                      />
                    </div>
                  )}

                  {selectedType.name === "Restriction" && (
                    <div className="space-y-1.5">
                      <label className="text-xs font-medium text-muted-foreground">{t("stepConfig.enzymes")} *</label>
                      <input
                        type="text"
                        value={((stepConfig.enzymes as string[]) ?? []).join(", ")}
                        onChange={(e) => setStepConfig(c => ({ ...c, enzymes: e.target.value.split(",").map(s => s.trim()).filter(Boolean) }))}
                        placeholder="e.g., EcoRI, BamHI, HindIII"
                        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                      />
                      <p className="text-xs text-muted-foreground">{t("stepConfig.enzymesSeparator")}</p>
                    </div>
                  )}
                </div>
              );
            })()}
          </div>
          <DialogFooter>
            <button
              onClick={() => {
                setAddStepDialogOpen(false);
                setSelectedStepType(null);
                setStepConfig({});
              }}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleAddStep}
              disabled={!selectedStepType || addStep.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {addStep.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : t("addStep.add")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Execute Pipeline Dialog */}
      <Dialog open={executeDialogOpen} onOpenChange={setExecuteDialogOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("execute.title")}</DialogTitle>
            <DialogDescription>{t("execute.description")}</DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">{t("execute.selectTrace")}</label>
              {processedTraces.length === 0 ? (
                <div className="rounded-lg border border-dashed border-border bg-muted/20 p-4 text-center">
                  <p className="text-sm text-muted-foreground">{t("execute.noProcessedTraces")}</p>
                </div>
              ) : (
                <div className="max-h-64 space-y-1.5 overflow-y-auto">
                  {processedTraces.map((trace) => (
                    <button
                      key={trace.id}
                      onClick={() => setSelectedTraceId(trace.id)}
                      className={cn(
                        "flex w-full items-center gap-3 rounded-lg border p-3 text-left transition-all",
                        selectedTraceId === trace.id
                          ? "border-teal bg-teal/5"
                          : "border-border hover:border-teal/50"
                      )}
                    >
                      <Dna className="h-4 w-4 text-teal" />
                      <span className="flex-1 text-sm font-medium text-foreground">{trace.name}</span>
                      {selectedTraceId === trace.id && (
                        <CheckCircle2 className="h-4 w-4 text-teal" />
                      )}
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => {
                setExecuteDialogOpen(false);
                setSelectedTraceId("");
              }}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleExecutePipeline}
              disabled={!selectedTraceId || executePipeline.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {executePipeline.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : t("execute.run")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
