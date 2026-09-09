-- Seed more studies distributed across different users

INSERT INTO studies.studies (id, owner_id, title, description, research_field, status, institution, principal_investigator, tags, created_at, created_by, is_deleted)
VALUES
-- Sarah Chen (U00000009) - Marine Biology
('S00000021', 'U00000009', 'Zebrafish Embryo Development Atlas', 'Comprehensive imaging and sequencing atlas of zebrafish embryonic development stages.', 'Genomics', 'Active', 'Marine Biological Laboratory', 'Dr. Sarah Chen', ARRAY['zebrafish', 'development', 'atlas'], NOW() - INTERVAL '30 days', 'U00000009', false),
('S00000022', 'U00000009', 'CRISPR Knockout Library Screening', 'High-throughput CRISPR knockout screening in marine model organisms.', 'Genomics', 'Active', 'Marine Biological Laboratory', 'Dr. Sarah Chen', ARRAY['crispr', 'screening', 'marine'], NOW() - INTERVAL '15 days', 'U00000009', false),

-- Michael Green (U00000010) - Plant Sciences
('S00000023', 'U00000010', 'Drought Tolerance Gene Discovery', 'Identification of novel drought tolerance genes in Arabidopsis using GWAS.', 'Genomics', 'Active', 'Plant Sciences Institute', 'Dr. Michael Green', ARRAY['drought', 'gwas', 'arabidopsis'], NOW() - INTERVAL '45 days', 'U00000010', false),
('S00000024', 'U00000010', 'Root Microbiome Interactions', 'Study of plant-microbiome interactions in the rhizosphere under stress conditions.', 'Metagenomics', 'Draft', 'Plant Sciences Institute', 'Dr. Michael Green', ARRAY['microbiome', 'roots', 'stress'], NOW() - INTERVAL '5 days', 'U00000010', false),

-- Emma Wilson (U00000011) - Genetics
('S00000025', 'U00000011', 'European Population Structure Analysis', 'Fine-scale population structure analysis using ancient and modern DNA.', 'Genetics', 'Completed', 'Genetics Research Center', 'Dr. Emma Wilson', ARRAY['population', 'ancient-dna', 'europe'], NOW() - INTERVAL '90 days', 'U00000011', false),
('S00000026', 'U00000011', 'Neanderthal Introgression Mapping', 'Mapping Neanderthal genetic contributions to modern human genomes.', 'Genetics', 'Active', 'Genetics Research Center', 'Dr. Emma Wilson', ARRAY['neanderthal', 'introgression', 'evolution'], NOW() - INTERVAL '20 days', 'U00000011', false),

-- James Liu (U00000012) - Bioinformatics
('S00000027', 'U00000012', 'Long-Read Sequencing Pipeline', 'Development of analysis pipeline for PacBio and Oxford Nanopore data.', 'Bioinformatics', 'Active', 'Sequencing Core Facility', 'Dr. James Liu', ARRAY['long-read', 'pacbio', 'nanopore'], NOW() - INTERVAL '60 days', 'U00000012', false),
('S00000028', 'U00000012', 'Variant Calling Benchmarking', 'Comprehensive benchmarking of variant calling algorithms on diverse datasets.', 'Bioinformatics', 'Active', 'Sequencing Core Facility', 'Dr. James Liu', ARRAY['variant-calling', 'benchmarking', 'ngs'], NOW() - INTERVAL '10 days', 'U00000012', false),

-- Ana Martinez (U00000013) - Proteomics
('S00000029', 'U00000013', 'Hypoxia Proteome Profiling', 'Mass spectrometry-based proteome profiling of cancer cells under hypoxia.', 'Proteomics', 'Active', 'Cancer Research Institute', 'Dr. Ana Martinez', ARRAY['hypoxia', 'mass-spec', 'cancer'], NOW() - INTERVAL '25 days', 'U00000013', false),
('S00000030', 'U00000013', 'Tumor Microenvironment Secretome', 'Analysis of secreted proteins in the tumor microenvironment.', 'Proteomics', 'Draft', 'Cancer Research Institute', 'Dr. Ana Martinez', ARRAY['secretome', 'tumor', 'microenvironment'], NOW() - INTERVAL '3 days', 'U00000013', false),

-- Robert Brown (U00000014) - Metagenomics
('S00000031', 'U00000014', 'Agricultural Soil Health Indicators', 'Metagenomic indicators of soil health in sustainable farming practices.', 'Metagenomics', 'Active', 'Environmental Sciences Lab', 'Dr. Robert Brown', ARRAY['soil', 'agriculture', 'sustainability'], NOW() - INTERVAL '40 days', 'U00000014', false),
('S00000032', 'U00000014', 'Forest Floor Carbon Cycling', 'Microbial communities involved in forest floor carbon cycling.', 'Metagenomics', 'Active', 'Environmental Sciences Lab', 'Dr. Robert Brown', ARRAY['forest', 'carbon', 'cycling'], NOW() - INTERVAL '55 days', 'U00000014', false),

-- Lisa Wang (U00000015) - Phylogenetics
('S00000033', 'U00000015', 'Hydrothermal Vent Ecosystem Evolution', 'Phylogenetic analysis of chemosynthetic bacteria from hydrothermal vents.', 'Phylogenetics', 'Active', 'Oceanography Institute', 'Dr. Lisa Wang', ARRAY['hydrothermal', 'chemosynthesis', 'deep-sea'], NOW() - INTERVAL '70 days', 'U00000015', false),
('S00000034', 'U00000015', 'Coral Symbiont Diversity', 'Evolutionary relationships among coral-associated microorganisms.', 'Phylogenetics', 'Active', 'Oceanography Institute', 'Dr. Lisa Wang', ARRAY['coral', 'symbiosis', 'diversity'], NOW() - INTERVAL '35 days', 'U00000015', false),

-- David Kim (U00000016) - Transcriptomics
('S00000035', 'U00000016', 'Yeast Heat Shock Response', 'Single-cell RNA-seq analysis of yeast heat shock response dynamics.', 'Transcriptomics', 'Completed', 'Molecular Biology Lab', 'Dr. David Kim', ARRAY['yeast', 'heat-shock', 'single-cell'], NOW() - INTERVAL '80 days', 'U00000016', false),
('S00000036', 'U00000016', 'Oxidative Stress Transcriptome', 'Time-course transcriptomic analysis of oxidative stress in S. cerevisiae.', 'Transcriptomics', 'Active', 'Molecular Biology Lab', 'Dr. David Kim', ARRAY['oxidative-stress', 'time-course', 'yeast'], NOW() - INTERVAL '12 days', 'U00000016', false),

-- Jennifer Lee (U00000017) - Molecular Biology
('S00000037', 'U00000017', 'Base Editor Optimization', 'Optimization of cytosine and adenine base editors for therapeutic applications.', 'MolecularBiology', 'Active', 'Synthetic Biology Center', 'Dr. Jennifer Lee', ARRAY['base-editing', 'crispr', 'therapeutics'], NOW() - INTERVAL '28 days', 'U00000017', false),
('S00000038', 'U00000017', 'Prime Editing Delivery Systems', 'Development of novel delivery systems for prime editing in vivo.', 'MolecularBiology', 'Draft', 'Synthetic Biology Center', 'Dr. Jennifer Lee', ARRAY['prime-editing', 'delivery', 'gene-therapy'], NOW() - INTERVAL '7 days', 'U00000017', false),

-- Thomas Anderson (U00000018) - Genetics/Agriculture
('S00000039', 'U00000018', 'Wheat Rust Resistance Loci', 'Mapping of stem rust resistance loci in diverse wheat germplasm.', 'Genetics', 'Active', 'Agricultural Research Station', 'Dr. Thomas Anderson', ARRAY['wheat', 'rust', 'resistance'], NOW() - INTERVAL '50 days', 'U00000018', false),
('S00000040', 'U00000018', 'Rice Yield QTL Analysis', 'Quantitative trait loci analysis for rice yield under drought stress.', 'Genetics', 'Active', 'Agricultural Research Station', 'Dr. Thomas Anderson', ARRAY['rice', 'qtl', 'yield'], NOW() - INTERVAL '22 days', 'U00000018', false),

-- Maria Garcia (U00000019) - Metagenomics/Medical
('S00000041', 'U00000019', 'IBD Microbiome Signatures', 'Identification of microbial signatures distinguishing Crohn''s disease subtypes.', 'Metagenomics', 'Active', 'Medical Research Center', 'Dr. Maria Garcia', ARRAY['ibd', 'crohns', 'microbiome'], NOW() - INTERVAL '65 days', 'U00000019', false),
('S00000042', 'U00000019', 'Probiotic Intervention Study', 'Metagenomics analysis of probiotic intervention in ulcerative colitis patients.', 'Metagenomics', 'Active', 'Medical Research Center', 'Dr. Maria Garcia', ARRAY['probiotics', 'uc', 'clinical'], NOW() - INTERVAL '18 days', 'U00000019', false),

-- Susan Park (U00000021) - Virology
('S00000043', 'U00000021', 'Environmental Virome Discovery', 'Discovery of novel viruses from wastewater surveillance samples.', 'Genomics', 'Active', 'Virology Institute', 'Dr. Susan Park', ARRAY['virome', 'wastewater', 'surveillance'], NOW() - INTERVAL '38 days', 'U00000021', false),
('S00000044', 'U00000021', 'Phage-Bacteria Dynamics', 'Longitudinal study of bacteriophage-bacteria dynamics in environmental samples.', 'Genomics', 'Draft', 'Virology Institute', 'Dr. Susan Park', ARRAY['phage', 'bacteria', 'ecology'], NOW() - INTERVAL '8 days', 'U00000021', false),

-- Karen White (U00000023) - AMR/Transcriptomics
('S00000045', 'U00000023', 'Carbapenem Resistance Mechanisms', 'Transcriptomic analysis of carbapenem resistance in Enterobacteriaceae.', 'Transcriptomics', 'Active', 'Infectious Disease Center', 'Dr. Karen White', ARRAY['amr', 'carbapenem', 'enterobacteriaceae'], NOW() - INTERVAL '42 days', 'U00000023', false),
('S00000046', 'U00000023', 'Biofilm Gene Expression', 'Gene expression profiling of biofilm formation in multidrug-resistant pathogens.', 'Transcriptomics', 'Active', 'Infectious Disease Center', 'Dr. Karen White', ARRAY['biofilm', 'mdr', 'expression'], NOW() - INTERVAL '14 days', 'U00000023', false),

-- Chris Johnson (U00000026) - Epigenetics
('S00000047', 'U00000026', 'Pancreatic Cancer Methylome', 'Comprehensive DNA methylation profiling of pancreatic cancer progression.', 'Genomics', 'Active', 'Epigenetics Research Unit', 'Dr. Chris Johnson', ARRAY['methylation', 'pancreatic', 'progression'], NOW() - INTERVAL '75 days', 'U00000026', false),
('S00000048', 'U00000026', 'Histone Modification Atlas', 'ChIP-seq atlas of histone modifications in normal vs tumor tissues.', 'Genomics', 'Active', 'Epigenetics Research Unit', 'Dr. Chris Johnson', ARRAY['histone', 'chip-seq', 'epigenetics'], NOW() - INTERVAL '33 days', 'U00000026', false),

-- Mark Wilson (U00000028) - Bioinformatics
('S00000049', 'U00000028', 'Machine Learning Variant Classifier', 'Deep learning model for pathogenic variant classification.', 'Bioinformatics', 'Active', 'Bioinformatics Core', 'Dr. Mark Wilson', ARRAY['machine-learning', 'variant', 'classification'], NOW() - INTERVAL '48 days', 'U00000028', false),
('S00000050', 'U00000028', 'Single-Cell Analysis Toolkit', 'Development of integrated toolkit for single-cell RNA-seq analysis.', 'Bioinformatics', 'Active', 'Bioinformatics Core', 'Dr. Mark Wilson', ARRAY['single-cell', 'toolkit', 'rnaseq'], NOW() - INTERVAL '16 days', 'U00000028', false);

-- Insert study members (owners)
INSERT INTO studies.study_members (study_id, user_id, role, joined_at)
SELECT id, owner_id, 'Owner', created_at
FROM studies.studies
WHERE id >= 'S00000021' AND id <= 'S00000050';

-- Add some collaborators to make it more realistic
INSERT INTO studies.study_members (study_id, user_id, role, joined_at)
VALUES
-- Add James Liu as collaborator to Sarah Chen's study
('S00000021', 'U00000012', 'Collaborator', NOW() - INTERVAL '25 days'),
-- Add Ana Martinez to Emma Wilson's study
('S00000025', 'U00000013', 'Collaborator', NOW() - INTERVAL '85 days'),
-- Add Mark Wilson to James Liu's pipeline study
('S00000027', 'U00000028', 'Admin', NOW() - INTERVAL '55 days'),
-- Add David Kim to Jennifer Lee's study
('S00000037', 'U00000016', 'Collaborator', NOW() - INTERVAL '20 days'),
-- Add Robert Brown to Maria Garcia's microbiome study
('S00000041', 'U00000014', 'Collaborator', NOW() - INTERVAL '60 days'),
-- Add Lisa Wang to Susan Park's virome study
('S00000043', 'U00000015', 'Collaborator', NOW() - INTERVAL '30 days');
