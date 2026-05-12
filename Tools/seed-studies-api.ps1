# Script to seed example studies via API
# Run with: powershell -ExecutionPolicy Bypass -File Tools\seed-studies-api.ps1

param(
    [string]$BaseUrl = "http://localhost:5173",
    [string]$Email = "test@example.com",
    [string]$Password = "Test123!"
)

# Research fields mapping (1-9)
$researchFields = @{
    1 = "Genomics"
    2 = "Proteomics"
    3 = "Transcriptomics"
    4 = "Metagenomics"
    5 = "Phylogenetics"
    6 = "Molecular Biology"
    7 = "Genetics"
    8 = "Bioinformatics"
}

# Example studies with varied data
$studies = @(
    @{ title = "CRISPR Gene Editing in Zebrafish"; description = "Analyzing CRISPR-Cas9 gene editing efficiency in zebrafish embryos for developmental biology research."; field = 1; institution = "Marine Biological Laboratory"; pi = "Dr. Sarah Chen" },
    @{ title = "Whole Genome Sequencing of Arabidopsis"; description = "Complete genome sequencing of Arabidopsis thaliana variants to identify SNPs associated with drought resistance."; field = 1; institution = "Plant Sciences Institute"; pi = "Dr. Michael Green" },
    @{ title = "Mitochondrial DNA Analysis"; description = "Population-level study of mitochondrial DNA variations in European populations for ancestry research."; field = 7; institution = "Genetics Research Center"; pi = "Dr. Emma Wilson" },
    @{ title = "Sanger Sequencing Validation"; description = "Validation pipeline for Sanger sequencing results comparing manual and automated analysis methods."; field = 8; institution = "Sequencing Core Facility"; pi = "Dr. James Liu" },
    @{ title = "Novel Protein Expression Study"; description = "Investigating novel protein expression patterns in cancer cell lines under hypoxic conditions."; field = 2; institution = "Cancer Research Institute"; pi = "Dr. Ana Martinez" },
    @{ title = "Soil Microbiome Metagenomics"; description = "Metagenomic analysis of soil microbial communities in agricultural vs. forest environments."; field = 4; institution = "Environmental Sciences Lab"; pi = "Dr. Robert Brown" },
    @{ title = "Marine Bacteria Phylogeny"; description = "Evolutionary phylogenetic analysis of marine bacteria from deep sea hydrothermal vents."; field = 5; institution = "Oceanography Institute"; pi = "Dr. Lisa Wang" },
    @{ title = "Stress Response RNA-Seq"; description = "RNA-Seq analysis of stress response genes in yeast under various environmental conditions."; field = 3; institution = "Molecular Biology Lab"; pi = "Dr. David Kim" },
    @{ title = "Plasmid Verification Project"; description = "Construction and verification of CRISPR plasmids for gene editing applications."; field = 6; institution = "Synthetic Biology Center"; pi = "Dr. Jennifer Lee" },
    @{ title = "Agricultural SNP Detection"; description = "SNP detection in wheat and rice cultivars for marker-assisted breeding programs."; field = 7; institution = "Agricultural Research Station"; pi = "Dr. Thomas Anderson" },
    @{ title = "Gut Microbiome Survey"; description = "Comprehensive metagenomic survey of human gut microbiome in healthy vs. IBD patients."; field = 4; institution = "Medical Research Center"; pi = "Dr. Maria Garcia" },
    @{ title = "Chloroplast Assembly"; description = "De novo assembly of chloroplast genomes from various plant species for comparative analysis."; field = 1; institution = "Botanical Gardens Research"; pi = "Dr. Peter Zhang" },
    @{ title = "Viral Characterization"; description = "Genomic characterization of novel viral strains isolated from environmental samples."; field = 1; institution = "Virology Institute"; pi = "Dr. Susan Park" },
    @{ title = "PCR Primer Optimization"; description = "Systematic optimization of PCR primers for diagnostic applications in clinical settings."; field = 6; institution = "Diagnostic Lab"; pi = "Dr. John Smith" },
    @{ title = "Drug Resistance Transcriptomics"; description = "Transcriptomic profiling of drug resistance mechanisms in bacterial pathogens."; field = 3; institution = "Infectious Disease Center"; pi = "Dr. Karen White" },
    @{ title = "Endemic Species Population Genetics"; description = "Population genetics study of endemic species in isolated island ecosystems."; field = 7; institution = "Conservation Biology Lab"; pi = "Dr. Daniel Moore" },
    @{ title = "16S rRNA Bacterial Analysis"; description = "16S rRNA gene sequencing for bacterial community profiling in wastewater treatment plants."; field = 4; institution = "Environmental Engineering Dept"; pi = "Dr. Rachel Taylor" },
    @{ title = "Epigenetic Modification Study"; description = "Genome-wide mapping of epigenetic modifications in cancer vs. normal tissues."; field = 1; institution = "Epigenetics Research Unit"; pi = "Dr. Chris Johnson" },
    @{ title = "Yeast Comparative Genomics"; description = "Comparative genomics of industrial yeast strains for fermentation optimization."; field = 8; institution = "Biotechnology Institute"; pi = "Dr. Laura Davis" },
    @{ title = "NGS Quality Control Pipeline"; description = "Development and validation of quality control pipeline for next-generation sequencing data."; field = 8; institution = "Bioinformatics Core"; pi = "Dr. Mark Wilson" }
)

Write-Host "GeneFlow Study Seeding Script" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login to get token
Write-Host "Step 1: Logging in..." -ForegroundColor Yellow

try {
    $loginBody = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token

    if (-not $token) {
        Write-Host "Failed to get token from login response" -ForegroundColor Red
        exit 1
    }

    Write-Host "Login successful!" -ForegroundColor Green
} catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the API is running and the credentials are correct." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "You can also register a new user first with:" -ForegroundColor Yellow
    Write-Host "  Invoke-RestMethod -Uri '$BaseUrl/api/v1/auth/register' -Method Post -Body '{`"email`":`"$Email`",`"password`":`"$Password`",`"fullName`":`"Test User`"}' -ContentType 'application/json'" -ForegroundColor Gray
    exit 1
}

# Step 2: Create studies
Write-Host ""
Write-Host "Step 2: Creating studies..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$created = 0
$failed = 0

foreach ($study in $studies) {
    try {
        $body = @{
            title = $study.title
            description = $study.description
            researchFieldId = $study.field
            institution = $study.institution
            principalInvestigator = $study.pi
            tags = @("research", $researchFields[$study.field].ToLower().Replace(" ", "-"))
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/studies" -Method Post -Headers $headers -Body $body

        Write-Host "  Created: $($study.title)" -ForegroundColor Green
        $created++
    } catch {
        Write-Host "  Failed: $($study.title) - $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }

    # Small delay to avoid overwhelming the API
    Start-Sleep -Milliseconds 100
}

Write-Host ""
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "Seeding complete!" -ForegroundColor Cyan
Write-Host "  Created: $created studies" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "  Failed: $failed studies" -ForegroundColor Red
}
