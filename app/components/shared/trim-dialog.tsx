"use client";

import { useState, useEffect } from "react";
import { Scissors, Loader2, AlertCircle, Trash2, Plus } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Button,
  Input,
  Badge,
} from "@/components/ui";
import { useAddTrim, useUndoTrim, useTraceTrims } from "@/hooks/use-traces";
import type { TraceTrim, AddTrimInput, TrimEndSymbol } from "@/types";

interface TrimDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  studyId: string;
  traceId: string;
  sequenceLength: number;
  existingTrims?: TraceTrim[];
  onSuccess?: (trim: TraceTrim) => void;
}

export function TrimDialog({
  open,
  onOpenChange,
  studyId,
  traceId,
  sequenceLength,
  existingTrims = [],
  onSuccess,
}: TrimDialogProps) {
  const [trimEnd, setTrimEnd] = useState<"FivePrime" | "ThreePrime">("FivePrime");
  const [startPosition, setStartPosition] = useState(0);
  const [endPosition, setEndPosition] = useState(0);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);

  const addTrimMutation = useAddTrim(studyId, traceId);
  const undoTrimMutation = useUndoTrim(studyId, traceId);
  const { data: trims, refetch: refetchTrims } = useTraceTrims(studyId, traceId, true);

  // Use passed trims or fetched trims
  const activeTrims = trims ?? existingTrims.filter(t => t.isActive);

  // Calculate trimmed stats
  const totalTrimmed = activeTrims.reduce((acc, t) => acc + t.length, 0);
  const remainingLength = sequenceLength - totalTrimmed;

  // Initialize defaults when opening
  useEffect(() => {
    if (open) {
      setTrimEnd("FivePrime");
      setStartPosition(0);
      setEndPosition(50);
      setReason("");
      setError(null);
    }
  }, [open]);

  // Update positions when trim end changes
  useEffect(() => {
    if (trimEnd === "FivePrime") {
      setStartPosition(0);
      setEndPosition(50);
    } else {
      setStartPosition(Math.max(0, sequenceLength - 50));
      setEndPosition(sequenceLength);
    }
  }, [trimEnd, sequenceLength]);

  const trimLength = endPosition - startPosition;
  const isValid =
    startPosition >= 0 &&
    endPosition <= sequenceLength &&
    startPosition < endPosition &&
    trimLength > 0;

  const handleSubmit = async () => {
    if (!isValid) {
      setError("Invalid trim positions. End must be greater than start.");
      return;
    }

    setError(null);

    const input: AddTrimInput = {
      startPosition,
      endPosition,
      trimEnd,
      reason: reason.trim() || undefined,
    };

    try {
      const result = await addTrimMutation.mutateAsync(input);
      onSuccess?.(result);
      refetchTrims();
      // Reset form for another trim
      setReason("");
      if (trimEnd === "FivePrime") {
        setStartPosition(0);
        setEndPosition(50);
      } else {
        setStartPosition(Math.max(0, sequenceLength - 50));
        setEndPosition(sequenceLength);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to apply trim");
    }
  };

  const handleUndoTrim = async (trimId: string) => {
    try {
      await undoTrimMutation.mutateAsync(trimId);
      refetchTrims();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to undo trim");
    }
  };

  const handleClose = () => {
    onOpenChange(false);
  };

  const getTrimEndSymbol = (trimEndId: number): string => {
    return trimEndId === 1 ? "5'" : "3'";
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[550px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Scissors className="h-5 w-5 text-teal" />
            Trim Sequence
          </DialogTitle>
          <DialogDescription>
            Add trim regions to exclude low-quality bases from analysis. Multiple trims can be applied.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Sequence info */}
          <div className="flex items-center justify-between rounded-lg bg-muted/50 p-3">
            <span className="text-sm text-muted-foreground">Original length</span>
            <span className="font-mono text-sm font-medium">{sequenceLength} bp</span>
          </div>

          {/* Active Trims List */}
          {activeTrims.length > 0 && (
            <div className="space-y-2">
              <h4 className="text-sm font-medium text-foreground">Active Trims</h4>
              <div className="space-y-2">
                {activeTrims.map((trim) => (
                  <div
                    key={trim.id}
                    className="flex items-center justify-between rounded-lg border bg-muted/30 p-2"
                  >
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="font-mono text-xs">
                        {getTrimEndSymbol(trim.trimEndId)}
                      </Badge>
                      <span className="font-mono text-sm">
                        {trim.startPosition}-{trim.endPosition}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        ({trim.length} bp)
                      </span>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleUndoTrim(trim.id)}
                      disabled={undoTrimMutation.isPending}
                      className="h-7 w-7 p-0 text-muted-foreground hover:text-red-500"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Remaining after trims</span>
                <span className="font-mono font-medium text-teal">{remainingLength} bp</span>
              </div>
            </div>
          )}

          {/* Add New Trim */}
          <div className="space-y-4 rounded-lg border p-4">
            <div className="flex items-center gap-2">
              <Plus className="h-4 w-4 text-muted-foreground" />
              <h4 className="text-sm font-medium">Add Trim</h4>
            </div>

            {/* Trim End Selection */}
            <div className="flex gap-2">
              <Button
                type="button"
                variant={trimEnd === "FivePrime" ? "default" : "outline"}
                size="sm"
                onClick={() => setTrimEnd("FivePrime")}
                className={trimEnd === "FivePrime" ? "bg-teal hover:bg-teal/90" : ""}
              >
                5&apos; End
              </Button>
              <Button
                type="button"
                variant={trimEnd === "ThreePrime" ? "default" : "outline"}
                size="sm"
                onClick={() => setTrimEnd("ThreePrime")}
                className={trimEnd === "ThreePrime" ? "bg-teal hover:bg-teal/90" : ""}
              >
                3&apos; End
              </Button>
            </div>

            {/* Position Inputs */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs text-muted-foreground">Start Position</label>
                <Input
                  type="number"
                  min={0}
                  max={sequenceLength}
                  value={startPosition}
                  onChange={(e) => setStartPosition(Number(e.target.value))}
                  className="font-mono"
                />
              </div>
              <div className="space-y-1.5">
                <label className="text-xs text-muted-foreground">End Position</label>
                <Input
                  type="number"
                  min={0}
                  max={sequenceLength}
                  value={endPosition}
                  onChange={(e) => setEndPosition(Number(e.target.value))}
                  className="font-mono"
                />
              </div>
            </div>

            {/* Reason (optional) */}
            <div className="space-y-1.5">
              <label className="text-xs text-muted-foreground">Reason (optional)</label>
              <Input
                type="text"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="e.g., Low quality region"
                maxLength={500}
              />
            </div>

            {/* Trim Preview */}
            <div className="flex items-center justify-between rounded bg-muted/50 p-2">
              <span className="text-xs text-muted-foreground">
                Will remove {isValid ? trimLength : 0} bases from {trimEnd === "FivePrime" ? "5'" : "3'"} end
              </span>
              <Button
                size="sm"
                onClick={handleSubmit}
                disabled={!isValid || addTrimMutation.isPending}
                className="bg-teal hover:bg-teal/90"
              >
                {addTrimMutation.isPending && <Loader2 className="mr-2 h-3 w-3 animate-spin" />}
                Add Trim
              </Button>
            </div>
          </div>

          {/* Visual preview bar */}
          {sequenceLength > 0 && (
            <div className="space-y-2">
              <div className="h-3 overflow-hidden rounded-full bg-muted relative">
                {/* Show active trims as darker regions */}
                {activeTrims.map((trim) => (
                  <div
                    key={trim.id}
                    className="absolute h-full bg-red-400/60"
                    style={{
                      left: `${(trim.startPosition / sequenceLength) * 100}%`,
                      width: `${(trim.length / sequenceLength) * 100}%`,
                    }}
                  />
                ))}
                {/* Show pending trim preview */}
                {isValid && (
                  <div
                    className="absolute h-full bg-teal/40 border-2 border-teal border-dashed"
                    style={{
                      left: `${(startPosition / sequenceLength) * 100}%`,
                      width: `${(trimLength / sequenceLength) * 100}%`,
                    }}
                  />
                )}
              </div>
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>0</span>
                <span>{sequenceLength} bp</span>
              </div>
            </div>
          )}

          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-500">
              <AlertCircle className="h-4 w-4 flex-shrink-0" />
              {error}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose}>
            Done
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
