-- Seed users and profiles
-- Password hash for "Test123!" using BCrypt

INSERT INTO identity.users (id, email, username, password_hash, is_active, email_verified, failed_login_attempts, two_factor_enabled, roles, created_at, is_deleted)
VALUES
('U00000009', 'sarah.chen@marine-lab.edu', 'sarahchen', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000010', 'michael.green@plantsciences.org', 'mgreen', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000011', 'emma.wilson@genetics-center.edu', 'ewilson', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000012', 'james.liu@sequencing.edu', 'jliu', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000013', 'ana.martinez@cancer-institute.org', 'amartinez', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000014', 'robert.brown@enviro-lab.edu', 'rbrown', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000015', 'lisa.wang@oceanography.edu', 'lwang', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000016', 'david.kim@molbio.edu', 'dkim', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000017', 'jennifer.lee@synbio.org', 'jlee', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000018', 'thomas.anderson@agri-research.edu', 'tanderson', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000019', 'maria.garcia@medical-center.org', 'mgarcia', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000020', 'peter.zhang@botanical.edu', 'pzhang', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000021', 'susan.park@virology.edu', 'spark', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000022', 'john.smith@diagnostics.org', 'jsmith', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000023', 'karen.white@infectious.edu', 'kwhite', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000024', 'daniel.moore@conservation.org', 'dmoore', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000025', 'rachel.taylor@enveng.edu', 'rtaylor', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000026', 'chris.johnson@epigenetics.org', 'cjohnson', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000027', 'laura.davis@biotech.edu', 'ldavis', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false),
('U00000028', 'mark.wilson@bioinformatics.edu', 'mwilson', '$2a$11$K8eVqmFN4.4.Uj4Gx1X9JeQz8DV4GQN5W1Y6G7YzR3mE8D7Qk9.Wy', true, true, 0, false, '["User"]', NOW(), false);

-- Insert profiles for new users
INSERT INTO profiles.profiles (id, user_id, first_name, last_name, bio, location, professional_role, institution_name, institution_department, research_field, created_at, is_deleted)
VALUES
('P00000007', 'U00000009', 'Sarah', 'Chen', 'Marine biologist specializing in developmental biology and CRISPR applications.', 'Woods Hole, MA', 'Senior Researcher', 'Marine Biological Laboratory', 'Developmental Biology', 'Genomics', NOW(), false),
('P00000008', 'U00000010', 'Michael', 'Green', 'Plant geneticist focused on drought resistance mechanisms.', 'Davis, CA', 'Associate Professor', 'Plant Sciences Institute', 'Plant Genetics', 'Genomics', NOW(), false),
('P00000009', 'U00000011', 'Emma', 'Wilson', 'Population geneticist studying human ancestry and migration patterns.', 'Cambridge, UK', 'Research Fellow', 'Genetics Research Center', 'Population Genetics', 'Genetics', NOW(), false),
('P00000010', 'U00000012', 'James', 'Liu', 'Bioinformatician developing sequencing analysis pipelines.', 'San Francisco, CA', 'Core Facility Director', 'Sequencing Core Facility', 'Bioinformatics', 'Bioinformatics', NOW(), false),
('P00000011', 'U00000013', 'Ana', 'Martinez', 'Cancer researcher studying protein expression in hypoxic conditions.', 'Houston, TX', 'Principal Investigator', 'Cancer Research Institute', 'Proteomics', 'Proteomics', NOW(), false),
('P00000012', 'U00000014', 'Robert', 'Brown', 'Environmental microbiologist studying soil ecosystems.', 'Madison, WI', 'Professor', 'Environmental Sciences Lab', 'Microbiology', 'Metagenomics', NOW(), false),
('P00000013', 'U00000015', 'Lisa', 'Wang', 'Marine microbiologist studying deep-sea bacterial evolution.', 'San Diego, CA', 'Assistant Professor', 'Oceanography Institute', 'Marine Biology', 'Phylogenetics', NOW(), false),
('P00000014', 'U00000016', 'David', 'Kim', 'Molecular biologist studying stress responses in yeast.', 'Boston, MA', 'Postdoctoral Fellow', 'Molecular Biology Lab', 'Molecular Biology', 'Transcriptomics', NOW(), false),
('P00000015', 'U00000017', 'Jennifer', 'Lee', 'Synthetic biologist developing CRISPR tools.', 'Berkeley, CA', 'Research Scientist', 'Synthetic Biology Center', 'Synthetic Biology', 'MolecularBiology', NOW(), false),
('P00000016', 'U00000018', 'Thomas', 'Anderson', 'Agricultural geneticist working on crop improvement.', 'Ames, IA', 'Research Director', 'Agricultural Research Station', 'Crop Genetics', 'Genetics', NOW(), false),
('P00000017', 'U00000019', 'Maria', 'Garcia', 'Physician-scientist studying gut microbiome and IBD.', 'Baltimore, MD', 'Clinical Researcher', 'Medical Research Center', 'Gastroenterology', 'Metagenomics', NOW(), false),
('P00000018', 'U00000020', 'Peter', 'Zhang', 'Botanist specializing in chloroplast genomics.', 'St. Louis, MO', 'Curator', 'Botanical Gardens Research', 'Plant Sciences', 'Genomics', NOW(), false),
('P00000019', 'U00000021', 'Susan', 'Park', 'Virologist characterizing novel environmental viruses.', 'Atlanta, GA', 'Senior Scientist', 'Virology Institute', 'Virology', 'Genomics', NOW(), false),
('P00000020', 'U00000022', 'John', 'Smith', 'Clinical scientist developing diagnostic PCR assays.', 'Rochester, MN', 'Lab Director', 'Diagnostic Lab', 'Clinical Diagnostics', 'MolecularBiology', NOW(), false),
('P00000021', 'U00000023', 'Karen', 'White', 'Infectious disease researcher studying antimicrobial resistance.', 'Seattle, WA', 'Associate Professor', 'Infectious Disease Center', 'Microbiology', 'Transcriptomics', NOW(), false),
('P00000022', 'U00000024', 'Daniel', 'Moore', 'Conservation biologist studying island endemic species.', 'Honolulu, HI', 'Research Fellow', 'Conservation Biology Lab', 'Conservation Genetics', 'Genetics', NOW(), false),
('P00000023', 'U00000025', 'Rachel', 'Taylor', 'Environmental engineer studying wastewater microbiomes.', 'Ann Arbor, MI', 'Assistant Professor', 'Environmental Engineering Dept', 'Environmental Microbiology', 'Metagenomics', NOW(), false),
('P00000024', 'U00000026', 'Chris', 'Johnson', 'Epigeneticist mapping cancer methylation patterns.', 'Philadelphia, PA', 'Principal Investigator', 'Epigenetics Research Unit', 'Epigenetics', 'Genomics', NOW(), false),
('P00000025', 'U00000027', 'Laura', 'Davis', 'Biotechnologist optimizing industrial yeast strains.', 'San Jose, CA', 'Senior Scientist', 'Biotechnology Institute', 'Industrial Biotechnology', 'Bioinformatics', NOW(), false),
('P00000026', 'U00000028', 'Mark', 'Wilson', 'Bioinformatician developing NGS quality control tools.', 'Durham, NC', 'Core Director', 'Bioinformatics Core', 'Computational Biology', 'Bioinformatics', NOW(), false);
