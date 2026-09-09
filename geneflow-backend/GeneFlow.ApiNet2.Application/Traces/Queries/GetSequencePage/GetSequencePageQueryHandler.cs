using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetSequencePage;

/// <summary>
/// Handler for GetSequencePageQuery.
/// Reads chunked sequence data from datalake storage and returns a paginated response.
/// </summary>
public sealed class GetSequencePageQueryHandler
    : IQueryHandler<GetSequencePageQuery, Result<SequencePageDto>>
{
    private readonly ITraceRepository _traceRepository;
    private readonly IDatalakeStorageClient _datalakeClient;

    public GetSequencePageQueryHandler(
        ITraceRepository traceRepository,
        IDatalakeStorageClient datalakeClient)
    {
        _traceRepository = traceRepository;
        _datalakeClient = datalakeClient;
    }

    public async Task<Result<SequencePageDto>> Handle(
        GetSequencePageQuery request,
        CancellationToken cancellationToken)
    {
        // Validate user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<SequencePageDto>(TraceErrors.InvalidUserId);

        // Validate trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<SequencePageDto>(TraceErrors.NotFound);

        // Validate pagination parameters
        if (request.Page < 1)
            return Result.Failure<SequencePageDto>(TraceErrors.InvalidPageNumber);

        if (request.PageSize < 100 || request.PageSize > 50000)
            return Result.Failure<SequencePageDto>(TraceErrors.InvalidPageSize);

        // Verify trace exists
        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<SequencePageDto>(TraceErrors.NotFound);

        // Get manifest from datalake
        var manifest = await _datalakeClient.GetManifestAsync(request.TraceId, cancellationToken);
        if (manifest is null)
            return Result.Failure<SequencePageDto>(TraceErrors.ChunkedDataNotAvailable);

        // Calculate which chunks we need for this page
        var startPosition = (request.Page - 1) * request.PageSize;
        var endPosition = startPosition + request.PageSize - 1;

        if (startPosition >= manifest.TotalBases)
            return Result.Failure<SequencePageDto>(TraceErrors.PageOutOfRange);

        // Adjust end position if it exceeds total bases
        endPosition = Math.Min(endPosition, manifest.TotalBases - 1);

        // Determine which chunks contain this range
        var startChunkIndex = startPosition / manifest.ChunkSize;
        var endChunkIndex = endPosition / manifest.ChunkSize;

        // Collect data from chunks
        var bases = new List<char>();
        var qualityScores = manifest.HasQualityScores ? new List<int>() : null;
        var chromatogramData = manifest.HasChromatogram
            ? new ChromatogramAccumulator()
            : null;

        for (var chunkIndex = startChunkIndex; chunkIndex <= endChunkIndex; chunkIndex++)
        {
            var chunk = await _datalakeClient.GetChunkAsync(
                request.TraceId,
                chunkIndex,
                cancellationToken);

            if (chunk is null)
                continue;

            // Calculate the range within this chunk that we need
            var chunkStart = chunkIndex * manifest.ChunkSize;
            var relativeStart = Math.Max(0, startPosition - chunkStart);
            var relativeEnd = Math.Min(chunk.Bases.Length - 1, endPosition - chunkStart);
            var length = relativeEnd - relativeStart + 1;

            // Extract bases
            bases.AddRange(chunk.Bases.Substring(relativeStart, length));

            // Extract quality scores if available
            if (qualityScores is not null && chunk.QualityScores is not null)
            {
                qualityScores.AddRange(
                    chunk.QualityScores.Skip(relativeStart).Take(length));
            }

            // Extract chromatogram data if available
            if (chromatogramData is not null && chunk.Chromatogram is not null)
            {
                chromatogramData.Append(chunk.Chromatogram, relativeStart, length);
            }
        }

        var totalPages = (int)Math.Ceiling((double)manifest.TotalBases / request.PageSize);

        var result = new SequencePageDto(
            TraceId: request.TraceId,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalBases: manifest.TotalBases,
            TotalPages: totalPages,
            Bases: new string(bases.ToArray()),
            QualityScores: qualityScores,
            Chromatogram: chromatogramData?.ToDto());

        return Result.Success(result);
    }

    /// <summary>
    /// Helper class to accumulate chromatogram data across chunks.
    /// </summary>
    private sealed class ChromatogramAccumulator
    {
        private readonly List<int> _aChannel = new();
        private readonly List<int> _tChannel = new();
        private readonly List<int> _gChannel = new();
        private readonly List<int> _cChannel = new();
        private readonly List<int> _peakPositions = new();

        public void Append(ChromatogramChunkDto chunk, int start, int length)
        {
            // For chromatogram, we need to handle the data points corresponding to our bases
            // Each base typically has multiple data points, so we need the peak positions
            // to know which data points correspond to which bases

            if (chunk.PeakPositions is not null && chunk.PeakPositions.Count > 0)
            {
                var startIdx = start < chunk.PeakPositions.Count ? chunk.PeakPositions[start] : 0;
                var endIdx = (start + length - 1) < chunk.PeakPositions.Count
                    ? chunk.PeakPositions[start + length - 1] + 1
                    : chunk.A?.Count ?? 0;

                var dataLength = endIdx - startIdx;

                if (chunk.A is not null)
                    _aChannel.AddRange(chunk.A.Skip(startIdx).Take(dataLength));
                if (chunk.T is not null)
                    _tChannel.AddRange(chunk.T.Skip(startIdx).Take(dataLength));
                if (chunk.G is not null)
                    _gChannel.AddRange(chunk.G.Skip(startIdx).Take(dataLength));
                if (chunk.C is not null)
                    _cChannel.AddRange(chunk.C.Skip(startIdx).Take(dataLength));

                // Adjust peak positions relative to the new start
                var adjustedPeaks = chunk.PeakPositions
                    .Skip(start)
                    .Take(length)
                    .Select(p => p - startIdx + _peakPositions.Count);
                _peakPositions.AddRange(adjustedPeaks);
            }
        }

        public ChromatogramChunkDto ToDto()
        {
            return new ChromatogramChunkDto(
                A: _aChannel.Count > 0 ? _aChannel : null,
                C: _cChannel.Count > 0 ? _cChannel : null,
                G: _gChannel.Count > 0 ? _gChannel : null,
                T: _tChannel.Count > 0 ? _tChannel : null,
                PeakPositions: _peakPositions.Count > 0 ? _peakPositions : null);
        }
    }
}
