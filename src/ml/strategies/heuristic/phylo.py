"""Heuristic strategies for phylogenetic analysis."""

from ..base import ModelStrategy, StrategyResult, StrategyType


class HeuristicClusterStrategy(ModelStrategy):
    """Rule-based sequence clustering using k-mer similarity."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_cluster"
    model_version = "1.0.0"

    async def execute(
        self,
        sequences: list[str],
        k: int = 6,
        threshold: float = 0.7,
        **kwargs,
    ) -> StrategyResult:
        """Cluster sequences by k-mer similarity."""
        if not sequences:
            return StrategyResult(
                data={"clusters": [], "totalClusters": 0},
                confidence=0.0,
                strategy_used=self.strategy_type,
                model_name=self.model_name,
                model_version=self.model_version,
            )

        # Generate k-mer profiles
        profiles = [self._get_kmer_profile(seq, k) for seq in sequences]

        # Simple greedy clustering
        clusters = []
        assigned = set()

        for i, prof_i in enumerate(profiles):
            if i in assigned:
                continue

            cluster = [i]
            assigned.add(i)

            for j, prof_j in enumerate(profiles):
                if j in assigned:
                    continue
                similarity = self._jaccard_similarity(prof_i, prof_j)
                if similarity >= threshold:
                    cluster.append(j)
                    assigned.add(j)

            clusters.append(cluster)

        return StrategyResult(
            data={
                "clusters": clusters,
                "totalClusters": len(clusters),
                "clusterSizes": [len(c) for c in clusters],
            },
            confidence=0.7,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )

    def _get_kmer_profile(self, seq: str, k: int) -> set[str]:
        seq = seq.upper()
        return {seq[i : i + k] for i in range(len(seq) - k + 1)}

    def _jaccard_similarity(self, set_a: set, set_b: set) -> float:
        if not set_a or not set_b:
            return 0.0
        intersection = len(set_a & set_b)
        union = len(set_a | set_b)
        return intersection / union if union > 0 else 0.0


class HeuristicDiversityStrategy(ModelStrategy):
    """Rule-based genetic diversity calculation."""

    strategy_type = StrategyType.HEURISTIC
    model_name = "heuristic_diversity"
    model_version = "1.0.0"

    async def execute(
        self,
        sequences: list[str],
        **kwargs,
    ) -> StrategyResult:
        """Calculate genetic diversity metrics."""
        if len(sequences) < 2:
            return StrategyResult(
                data={"error": "Need at least 2 sequences"},
                confidence=0.0,
                strategy_used=self.strategy_type,
                model_name=self.model_name,
                model_version=self.model_version,
            )

        seqs = [s.upper() for s in sequences]
        n = len(seqs)
        L = min(len(s) for s in seqs)

        # Nucleotide diversity (pi)
        pi = self._calculate_pi(seqs, L, n)

        # Watterson's theta
        S = self._count_segregating_sites(seqs, L)
        a1 = sum(1 / i for i in range(1, n))
        theta_w = S / a1 if a1 > 0 else 0

        # Tajima's D (simplified)
        tajimas_d = pi - theta_w  # Simplified, not normalized

        # Haplotype diversity
        haplotypes = set(tuple(s[:L]) for s in seqs)
        h = len(haplotypes)
        hap_div = (n / (n - 1)) * (1 - sum((seqs.count(s) / n) ** 2 for s in set(seqs)))

        return StrategyResult(
            data={
                "nucleotideDiversity": round(pi, 6),
                "thetaWatterson": round(theta_w, 6),
                "tajimasD": round(tajimas_d, 6),
                "haplotypeDiversity": round(hap_div, 4),
                "segregatingSites": S,
                "numHaplotypes": h,
                "numSequences": n,
            },
            confidence=0.75,
            strategy_used=self.strategy_type,
            model_name=self.model_name,
            model_version=self.model_version,
        )

    def _calculate_pi(self, seqs: list[str], L: int, n: int) -> float:
        diffs = 0
        comparisons = 0
        for i in range(n):
            for j in range(i + 1, n):
                diffs += sum(1 for k in range(L) if seqs[i][k] != seqs[j][k])
                comparisons += 1
        return diffs / (comparisons * L) if comparisons * L > 0 else 0

    def _count_segregating_sites(self, seqs: list[str], L: int) -> int:
        S = 0
        for pos in range(L):
            bases = set(s[pos] for s in seqs if s[pos] in "ATCG")
            if len(bases) > 1:
                S += 1
        return S
