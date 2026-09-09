# Script to seed example studies in Redis Streams
# Run with: powershell -File Tools\seed-studies.ps1

$containerName = "geneflow-core-redis"

# Get current study sequence
$currentSeq = docker exec $containerName redis-cli GET "geneflow:seq:studies"
$startId = [int]$currentSeq + 1

# Research fields mapping
$researchFields = @(
    @{ id = 1; name = "Genomics"; displayName = "Genomics" },
    @{ id = 2; name = "Proteomics"; displayName = "Proteomics" },
    @{ id = 3; name = "Transcriptomics"; displayName = "Transcriptomics" },
    @{ id = 4; name = "Metagenomics"; displayName = "Metagenomics" },
    @{ id = 5; name = "Phylogenetics"; displayName = "Phylogenetics" },
    @{ id = 6; name = "MolecularBiology"; displayName = "Molecular Biology" },
    @{ id = 7; name = "Genetics"; displayName = "Genetics" },
    @{ id = 8; name = "Bioinformatics"; displayName = "Bioinformatics" }
)

# Example study titles with different research contexts
$studyTitles = @(
    "CRISPR Gene Editing Analysis in Zebrafish",
    "Whole Genome Sequencing of Arabidopsis Variants",
    "Mitochondrial DNA Study in Human Populations",
    "Sanger Sequencing Validation Pipeline",
    "Novel Protein Expression in Cancer Cells",
    "Microbial Community Analysis in Soil Samples",
    "Evolutionary Phylogeny of Marine Bacteria",
    "RNA-Seq Analysis of Stress Response Genes",
    "Plasmid Construction and Verification",
    "SNP Detection in Agricultural Species",
    "Metagenomic Survey of Gut Microbiome",
    "Chloroplast Genome Assembly Project",
    "Viral Genome Characterization Study",
    "Primer Design for PCR Amplification",
    "Transcriptomic Profile of Drug Resistance",
    "Population Genetics of Endemic Species",
    "Bacterial 16S rRNA Analysis",
    "Epigenetic Modification Mapping",
    "Comparative Genomics of Yeast Strains",
    "Next-Gen Sequencing Quality Control",
    "Protein-DNA Interaction Analysis",
    "Gene Expression in Plant Development",
    "Antibiotic Resistance Gene Detection",
    "Chromosome Walking in Model Organisms",
    "Structural Variant Analysis Pipeline"
)

# Owner IDs to distribute studies across users (using existing users 1-4)
$ownerIds = @(1, 4, 4, 1, 4)

Write-Host "Starting study seeding..." -ForegroundColor Cyan
Write-Host "Current sequence: $currentSeq" -ForegroundColor Yellow
Write-Host "Will create studies starting from ID: $startId" -ForegroundColor Yellow

$created = 0

foreach ($i in 0..($studyTitles.Count - 1)) {
    $studyId = $startId + $i
    $title = $studyTitles[$i]
    $ownerId = $ownerIds[$i % $ownerIds.Count]
    $field = $researchFields[$i % $researchFields.Count]
    $eventId = [guid]::NewGuid().ToString()
    $occurredAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")

    # Build the JSON data payload (snake_case as per the serialization settings)
    $data = @{
        study_id = @{ value = $studyId }
        title = $title
        owner_id = @{ value = $ownerId }
        research_field = @{
            display_name = $field.displayName
            id = $field.id
            name = $field.name
        }
        event_id = $eventId
        occurred_at = $occurredAt
    } | ConvertTo-Json -Compress

    # Escape for redis-cli
    $escapedData = $data.Replace('"', '\"')

    # Add to Redis stream
    $cmd = "XADD geneflow:events:studies * event_id `"$eventId`" event_type `"StudyCreatedEvent`" occurred_at `"$occurredAt`" data `"$escapedData`""

    docker exec $containerName redis-cli $cmd | Out-Null

    Write-Host "Created study S$($studyId.ToString("00000000")): $title" -ForegroundColor Green
    $created++
}

# Update the sequence counter
$newSeq = $startId + $studyTitles.Count - 1
docker exec $containerName redis-cli SET "geneflow:seq:studies" $newSeq | Out-Null

Write-Host ""
Write-Host "Seeding complete!" -ForegroundColor Cyan
Write-Host "Created $created new studies" -ForegroundColor Green
Write-Host "New sequence value: $newSeq" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next step: Rebuild the read model by running the sync command or restarting processors" -ForegroundColor Magenta
