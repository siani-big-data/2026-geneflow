"use client";

import { useState, useEffect } from "react";
import { MessageSquare, Loader2, AlertCircle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Button,
  Input,
  Switch,
} from "@/components/ui";
import { cn } from "@/lib/utils";
import { useCreateAnnotation, useUpdateAnnotation } from "@/hooks/use-traces";
import type { CreateAnnotationInput, UpdateAnnotationInput, TraceAnnotation } from "@/types";
import { AnnotationTypeId, AnnotationTypeName, AnnotationStrandId, AnnotationStrandSymbol } from "@/types";

interface AnnotateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  studyId: string;
  traceId: string;
  sequenceLength: number;
  initialStart?: number;
  initialEnd?: number;
  onSuccess?: (annotation: TraceAnnotation) => void;
  editingAnnotation?: TraceAnnotation | null;
}

const ANNOTATION_COLORS = [
  { value: "#10b981", label: "Green" },
  { value: "#3b82f6", label: "Blue" },
  { value: "#f59e0b", label: "Amber" },
  { value: "#ef4444", label: "Red" },
  { value: "#8b5cf6", label: "Purple" },
  { value: "#ec4899", label: "Pink" },
];

export function AnnotateDialog({
  open,
  onOpenChange,
  studyId,
  traceId,
  sequenceLength,
  initialStart = 0,
  initialEnd = 0,
  onSuccess,
  editingAnnotation,
}: AnnotateDialogProps) {
  const [label, setLabel] = useState("");
  const [description, setDescription] = useState("");
  const [typeId, setTypeId] = useState<number>(AnnotationTypeId.Region);
  const [strandId, setStrandId] = useState<number>(AnnotationStrandId.None);
  const [startPosition, setStartPosition] = useState(initialStart);
  const [endPosition, setEndPosition] = useState(initialEnd);
  const [color, setColor] = useState(ANNOTATION_COLORS[0].value);
  const [isShared, setIsShared] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isEditing = !!editingAnnotation;
  const createMutation = useCreateAnnotation(studyId, traceId);
  const updateMutation = useUpdateAnnotation(studyId, traceId);

  // Reset form when dialog opens or populate with editing data
  useEffect(() => {
    if (open) {
      if (editingAnnotation) {
        // Populate with existing annotation data
        setLabel(editingAnnotation.label);
        setDescription(editingAnnotation.description || "");
        setTypeId(editingAnnotation.typeId || AnnotationTypeId.Region);
        setStrandId(editingAnnotation.strandId || AnnotationStrandId.None);
        setStartPosition(editingAnnotation.startPosition);
        setEndPosition(editingAnnotation.endPosition);
        setColor(editingAnnotation.color || ANNOTATION_COLORS[0].value);
        setIsShared(editingAnnotation.isShared || false);
      } else {
        // Reset for new annotation
        setLabel("");
        setDescription("");
        setTypeId(AnnotationTypeId.Region);
        setStrandId(AnnotationStrandId.None);
        setStartPosition(initialStart);
        setEndPosition(initialEnd || initialStart + 10);
        setColor(ANNOTATION_COLORS[0].value);
        setIsShared(false);
      }
      setError(null);
    }
  }, [open, initialStart, initialEnd, editingAnnotation]);

  const isValid =
    label.trim().length > 0 &&
    startPosition >= 0 &&
    endPosition <= sequenceLength &&
    startPosition <= endPosition;

  const handleSubmit = async () => {
    if (!isValid) {
      setError("Please provide a label and valid positions.");
      return;
    }

    setError(null);

    try {
      if (isEditing && editingAnnotation) {
        // Update existing annotation (typeId cannot be changed)
        const input: UpdateAnnotationInput = {
          label: label.trim(),
          description: description.trim() || undefined,
          startPosition,
          endPosition: typeId === AnnotationTypeId.Point ? startPosition : endPosition,
          strandId,
          color,
          isShared,
        };
        const result = await updateMutation.mutateAsync({ annotationId: editingAnnotation.id, input });
        onSuccess?.(result);
      } else {
        // Create new annotation
        const input: CreateAnnotationInput = {
          typeId,
          label: label.trim(),
          description: description.trim() || undefined,
          startPosition,
          endPosition: typeId === AnnotationTypeId.Point ? startPosition : endPosition,
          strandId,
          color,
          isShared,
        };
        const result = await createMutation.mutateAsync(input);
        onSuccess?.(result);
      }
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : isEditing ? "Failed to update annotation" : "Failed to create annotation");
    }
  };

  const handleCancel = () => {
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[520px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-teal" />
            {isEditing ? "Edit Annotation" : "Create Annotation"}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? "Modify the annotation properties."
              : "Add an annotation to mark a region or point of interest in the sequence."}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-5 py-4">
          {/* Label */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">
              Label <span className="text-red-500">*</span>
            </label>
            <Input
              placeholder="e.g., Primer binding site, Mutation, etc."
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              maxLength={100}
            />
          </div>

          {/* Annotation Type */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground">Type</label>
            <div className="grid grid-cols-4 gap-2">
              {Object.entries(AnnotationTypeId).map(([name, id]) => (
                <button
                  key={id}
                  type="button"
                  onClick={() => setTypeId(id)}
                  className={cn(
                    "rounded-lg border px-3 py-2 text-sm font-medium transition-all",
                    typeId === id
                      ? "border-teal bg-teal text-white"
                      : "border-border text-muted-foreground hover:border-teal hover:text-foreground"
                  )}
                >
                  {name}
                </button>
              ))}
            </div>
          </div>

          {/* Position */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground">Position</label>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <label className="text-xs text-muted-foreground">Start</label>
                <Input
                  type="number"
                  min={0}
                  max={sequenceLength}
                  value={startPosition}
                  onChange={(e) => setStartPosition(Number(e.target.value))}
                  className="font-mono"
                />
              </div>
              {typeId !== AnnotationTypeId.Point && (
                <div className="space-y-1">
                  <label className="text-xs text-muted-foreground">End</label>
                  <Input
                    type="number"
                    min={0}
                    max={sequenceLength}
                    value={endPosition}
                    onChange={(e) => setEndPosition(Number(e.target.value))}
                    className="font-mono"
                  />
                </div>
              )}
            </div>
            {typeId !== AnnotationTypeId.Point && (
              <p className="text-xs text-muted-foreground">
                Length: {endPosition - startPosition} bp
              </p>
            )}
          </div>

          {/* Strand */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground">Strand</label>
            <div className="flex gap-2">
              {Object.entries(AnnotationStrandId).map(([name, id]) => (
                <button
                  key={id}
                  type="button"
                  onClick={() => setStrandId(id)}
                  className={cn(
                    "flex h-9 w-12 items-center justify-center rounded-lg border text-sm font-mono font-bold transition-all",
                    strandId === id
                      ? "border-teal bg-teal text-white"
                      : "border-border text-muted-foreground hover:border-teal hover:text-foreground"
                  )}
                  title={name}
                >
                  {AnnotationStrandSymbol[id]}
                </button>
              ))}
            </div>
          </div>

          {/* Color */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground">Color</label>
            <div className="flex gap-2">
              {ANNOTATION_COLORS.map((c) => (
                <button
                  key={c.value}
                  type="button"
                  onClick={() => setColor(c.value)}
                  className={cn(
                    "h-8 w-8 rounded-full border-2 transition-all",
                    color === c.value ? "border-foreground scale-110" : "border-transparent"
                  )}
                  style={{ backgroundColor: c.value }}
                  title={c.label}
                />
              ))}
            </div>
          </div>

          {/* Description */}
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Description</label>
            <textarea
              placeholder="Optional description or notes..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="flex min-h-[80px] w-full rounded-lg border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              maxLength={500}
            />
          </div>

          {/* Shared toggle */}
          <div className="flex items-center justify-between rounded-lg border border-border p-3">
            <div>
              <p className="text-sm font-medium text-foreground">Share with team</p>
              <p className="text-xs text-muted-foreground">
                Make this annotation visible to all study members
              </p>
            </div>
            <Switch
              checked={isShared}
              onCheckedChange={setIsShared}
            />
          </div>

          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-500">
              <AlertCircle className="h-4 w-4 flex-shrink-0" />
              {error}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!isValid || createMutation.isPending || updateMutation.isPending}
            className="bg-teal hover:bg-teal/90"
          >
            {(createMutation.isPending || updateMutation.isPending) && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isEditing ? "Save Changes" : "Create Annotation"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
