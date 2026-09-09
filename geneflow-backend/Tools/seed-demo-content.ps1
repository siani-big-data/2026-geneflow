#requires -Version 5.1
<#
    Seed adicional para los 5 estudios demo:
      1) status -> Published (publicamente visible)
      2) marca 3 como is_featured (remarkable)
      3) ajusta views_count y stars_count + filas en study_stars
      4) inserta 1 pipeline por estudio (con 2-3 steps)
      5) inserta 1-2 papers por estudio
#>
param(
    [string] $PgContainer = "geneflow-core-postgres",
    [string] $PgUser      = "geneflow",
    [string] $PgDatabase  = "geneflow"
)

$ErrorActionPreference = "Stop"

$sql = @"
-- 1) Status -> Published (publicamente visible) en todos
UPDATE studies.studies
SET status = 'Published', modified_at = NOW()
WHERE id IN ('S00000007','S00000008','S00000009','S00000010','S00000011');

-- 2) Tres remarkables (uno por owner)
UPDATE studies.studies SET is_featured = true,  modified_at = NOW()
WHERE id IN ('S00000007','S00000009','S00000011');
UPDATE studies.studies SET is_featured = false, modified_at = NOW()
WHERE id IN ('S00000008','S00000010');

-- 3) Visitas y estrellas (cached counts)
UPDATE studies.studies SET views_count = 247, stars_count = 12 WHERE id = 'S00000007';
UPDATE studies.studies SET views_count =  89, stars_count =  5 WHERE id = 'S00000008';
UPDATE studies.studies SET views_count = 412, stars_count = 18 WHERE id = 'S00000009';
UPDATE studies.studies SET views_count =  63, stars_count =  3 WHERE id = 'S00000010';
UPDATE studies.studies SET views_count = 178, stars_count =  9 WHERE id = 'S00000011';

-- Filas reales en study_stars (los demo users se starrean entre si)
DELETE FROM studies.study_stars WHERE study_id IN ('S00000007','S00000008','S00000009','S00000010','S00000011');
INSERT INTO studies.study_stars (id, study_id, user_id, starred_at) VALUES
    (gen_random_uuid(), 'S00000007', 'U00000002', NOW() - INTERVAL '6 days'),
    (gen_random_uuid(), 'S00000007', 'U00000003', NOW() - INTERVAL '4 days'),
    (gen_random_uuid(), 'S00000008', 'U00000002', NOW() - INTERVAL '3 days'),
    (gen_random_uuid(), 'S00000009', 'U00000001', NOW() - INTERVAL '8 days'),
    (gen_random_uuid(), 'S00000009', 'U00000003', NOW() - INTERVAL '5 days'),
    (gen_random_uuid(), 'S00000010', 'U00000003', NOW() - INTERVAL '2 days'),
    (gen_random_uuid(), 'S00000011', 'U00000001', NOW() - INTERVAL '7 days'),
    (gen_random_uuid(), 'S00000011', 'U00000002', NOW() - INTERVAL '1 day');

-- 4) Pipelines (uno por estudio). Prefix P, 8 digitos.
DELETE FROM pipelines.pipeline_steps WHERE pipeline_id IN ('P00000001','P00000002','P00000003','P00000004','P00000005');
DELETE FROM pipelines.pipelines      WHERE id          IN ('P00000001','P00000002','P00000003','P00000004','P00000005');

INSERT INTO pipelines.pipelines (id, study_id, owner_id, name, description, status, created_at, created_by, "IsDeleted") VALUES
    ('P00000001','S00000007','U00000001','Sanger QC + Trim','Quality control + Mott trimming sobre Sanger benchmark.','Active', NOW(),'U00000001', false),
    ('P00000002','S00000008','U00000001','Reproducibility demo','Pipeline de validacion sobre datasets sinteticos.','Active', NOW(),'U00000001', false),
    ('P00000003','S00000009','U00000002','Plant comparative genomics','Quality + Trim + ORF detection para genomica vegetal.','Active', NOW(),'U00000002', false),
    ('P00000004','S00000010','U00000002','Sanger archive baseline','Pipeline base para procesar el archivo docente.','Draft',  NOW(),'U00000002', false),
    ('P00000005','S00000011','U00000003','Marine biodiversity QC','Calidad + trimming + busqueda de motivos para muestras marinas.','Draft', NOW(),'U00000003', false);

INSERT INTO pipelines.pipeline_steps (id, pipeline_id, step_type, "order", label, configuration, is_enabled, created_at) VALUES
    -- P00000001
    (gen_random_uuid(), 'P00000001', 'Quality',    1, 'QC inicial', '{}',                                           true, NOW()),
    (gen_random_uuid(), 'P00000001', 'Trimming',   2, 'Mott 0.05',  '{"algorithm":"mott","cutoff":0.05}',           true, NOW()),
    -- P00000002
    (gen_random_uuid(), 'P00000002', 'Quality',    1, 'QC',         '{}',                                           true, NOW()),
    (gen_random_uuid(), 'P00000002', 'Translation',2, 'Frame +1',   '{"frame":1}',                                  true, NOW()),
    -- P00000003
    (gen_random_uuid(), 'P00000003', 'Quality',    1, 'QC',         '{}',                                           true, NOW()),
    (gen_random_uuid(), 'P00000003', 'Trimming',   2, 'Lucy strict','{"algorithm":"lucy","cutoff":0.1}',             true, NOW()),
    (gen_random_uuid(), 'P00000003', 'ORF',        3, 'ORF >= 100', '{"min_length":100,"frames":[1,2,3]}',          true, NOW()),
    -- P00000004
    (gen_random_uuid(), 'P00000004', 'Quality',    1, 'QC',         '{}',                                           true, NOW()),
    -- P00000005
    (gen_random_uuid(), 'P00000005', 'Quality',    1, 'QC',         '{}',                                           true, NOW()),
    (gen_random_uuid(), 'P00000005', 'Trimming',   2, 'Mott',       '{"algorithm":"mott","cutoff":0.05}',           true, NOW()),
    (gen_random_uuid(), 'P00000005', 'Motif',      3, 'COI primer', '{"pattern":"GGTCAACAAATCATAAAGATATTGG","search_complement":true}', true, NOW());

-- 5) Papers (1-2 por estudio). Prefix R, 8 digitos.
DELETE FROM studies.study_papers WHERE id IN ('R00000001','R00000002','R00000003','R00000004','R00000005','R00000006','R00000007','R00000008');

INSERT INTO studies.study_papers (id, title, authors, doi, abstract, journal, publication_year, study_id, created_at, created_by, is_deleted) VALUES
    ('R00000001',
     'Improved basecalling for Sanger sequencing using Mott trimming',
     'Marrero E.; Galvez J.',
     '10.1234/sanger.2024.001',
     'We compare Mott and Lucy trimming algorithms over a curated Sanger dataset and report quality and length trade-offs.',
     'Bioinformatics Advances', 2024,
     'S00000007', NOW(),'U00000001', false),

    ('R00000002',
     'A reference Sanger benchmark for trace processing pipelines',
     'Marrero E.; Caballero M.',
     '10.1234/sanger.2024.002',
     'We release a curated benchmark of Sanger chromatograms with hand-annotated quality.',
     'GigaScience', 2024,
     'S00000007', NOW(),'U00000001', false),

    ('R00000003',
     'Reproducible bioinformatics workflows with GeneFlow',
     'Marrero E.',
     '10.1234/geneflow.demo.2025',
     'GeneFlow provides reproducible, declarative pipelines with end-to-end provenance.',
     'Journal of Open Source Software', 2025,
     'S00000008', NOW(),'U00000001', false),

    ('R00000004',
     'Comparative genomics of Canarian endemic flora',
     'Galvez J.; Marrero E.',
     '10.1234/canarias.flora.2024',
     'We sequence and compare the chloroplast genomes of 12 Canarian endemic species.',
     'Molecular Ecology Resources', 2024,
     'S00000009', NOW(),'U00000002', false),

    ('R00000005',
     'Phylogeography of laurel forest plants in Macaronesia',
     'Galvez J.',
     '10.1234/canarias.flora.2023',
     'Phylogeographic patterns of Macaronesian laurel forest taxa from cpDNA markers.',
     'Frontiers in Plant Science', 2023,
     'S00000009', NOW(),'U00000002', false),

    ('R00000006',
     'A teaching archive of Sanger chromatograms for molecular biology courses',
     'Galvez J.; Caballero M.',
     '10.1234/teaching.sanger.2024',
     'Curated dataset of 200 chromatograms with phred quality and didactic annotations.',
     'Biochemistry and Molecular Biology Education', 2024,
     'S00000010', NOW(),'U00000002', false),

    ('R00000007',
     'COI metabarcoding of marine fauna along the Atlantic coast of the Canary Islands',
     'Caballero M.; Galvez J.',
     '10.1234/marine.coi.2025',
     'COI-based species identification of 47 marine specimens, with focus on cryptic species.',
     'Marine Biodiversity', 2025,
     'S00000011', NOW(),'U00000003', false),

    ('R00000008',
     'Endemic marine fish DNA barcoding pipeline',
     'Caballero M.',
     '10.1234/marine.barcoding.2024',
     'A reproducible pipeline for fish DNA barcoding from Sanger reads.',
     'Reviews in Fish Biology and Fisheries', 2024,
     'S00000011', NOW(),'U00000003', false);

-- Resumen
SELECT s.id, s.title, s.status, s.is_featured, s.views_count, s.stars_count,
       (SELECT COUNT(*) FROM pipelines.pipelines p WHERE p.study_id = s.id AND NOT p."IsDeleted") AS pipelines,
       (SELECT COUNT(*) FROM studies.study_papers pp WHERE pp.study_id = s.id AND NOT pp.is_deleted) AS papers,
       (SELECT COUNT(*) FROM studies.study_members m WHERE m.study_id = s.id) AS members
FROM studies.studies s
WHERE s.id IN ('S00000007','S00000008','S00000009','S00000010','S00000011')
ORDER BY s.id;
"@

Write-Host "Aplicando seed de contenido demo..." -ForegroundColor Cyan
$sql | docker exec -i $PgContainer psql -U $PgUser -d $PgDatabase -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) { throw "Seed fallo" }
Write-Host "OK" -ForegroundColor Green
