-- ============================================================================
-- carga_inicial.sql — SalesSample
-- Carga realista (contexto agronegócio): 3 Filiais, 10 Clientes, 100 Produtos
-- Moeda: BRL (R$) · IDs: gen_random_uuid() (GUIDs aleatórios reais)
--
-- ATENÇÃO: o script TRUNCA as tabelas e recarrega do zero.
-- Depois de rodar, faça SELECT para pegar os Ids reais pro curl.
-- ============================================================================

TRUNCATE "SaleItems", "Sales", "OutboxMessages", "InboxMessages",
         "Products", "Customers", "Branches" CASCADE;

-- ---------------------------------------------------------------------------
-- 3 Filiais
-- ---------------------------------------------------------------------------
INSERT INTO "Branches" ("Id", "Name", "IsActive")
SELECT
    gen_random_uuid(),
    f.nome,
    true
FROM (
    VALUES
        ('Filial São Paulo - SP'),
        ('Filial Ribeirão Preto - SP'),
        ('Filial Cuiabá - MT')
) AS f(nome);

-- ---------------------------------------------------------------------------
-- 10 Clientes (produtores, cooperativas e agroindústrias)
-- ---------------------------------------------------------------------------
INSERT INTO "Customers" ("Id", "Name")
SELECT
    gen_random_uuid(),
    c.nome
FROM (
    VALUES
        ('Fazenda Santa Fé'),
        ('Agropecuária Boa Safra Ltda'),
        ('Cooperativa Agro Vale'),
        ('Fazenda Três Irmãos'),
        ('Sítio Recanto Verde'),
        ('Agroindústria Cerrado S/A'),
        ('Fazenda Boa Esperança'),
        ('Grupo Rural Horizonte'),
        ('Estância Rio Claro'),
        ('Fazenda São João da Serra')
) AS c(nome);

-- ---------------------------------------------------------------------------
-- 100 Produtos (fertilizantes, sementes, defensivos, máquinas, pecuária, infra)
-- ---------------------------------------------------------------------------
INSERT INTO "Products" ("Id", "Name", "PriceAmount", "PriceCurrency")
SELECT
    gen_random_uuid(),
    p.nome,
    p.preco,
    'BRL'
FROM (
    VALUES
        -- Fertilizantes e corretivos (1-15)
        ('Fertilizante NPK 20-05-20 (50kg)', 189.90),
        ('Fertilizante NPK 10-10-10 (50kg)', 165.00),
        ('Ureia Agrícola 45% (50kg)', 145.50),
        ('Cloreto de Potássio 60% (50kg)', 178.00),
        ('Superfosfato Simples (50kg)', 98.90),
        ('Superfosfato Triplo (50kg)', 210.00),
        ('Fosfato Monoamônico MAP (50kg)', 235.50),
        ('Fosfato Diamônico DAP (50kg)', 249.90),
        ('Nitrato de Amônio (50kg)', 175.00),
        ('Sulfato de Amônio (50kg)', 120.00),
        ('Calcário Dolomítico (1t)', 89.90),
        ('Gesso Agrícola (1t)', 75.00),
        ('Fertilizante Orgânico Composto (25kg)', 59.90),
        ('Fertilizante Foliar (1L)', 45.00),
        ('Adubo NPK Líquido (5L)', 79.90),
        -- Sementes (16-30)
        ('Semente de Soja BRS 539 (40kg)', 320.00),
        ('Semente de Soja M 6410 (40kg)', 355.00),
        ('Semente de Milho Híbrido 30A37 (20kg)', 480.00),
        ('Semente de Milho AG 1051 (20kg)', 450.00),
        ('Semente de Algodão FM 975 (25kg)', 390.00),
        ('Semente de Feijão Carioca (40kg)', 280.00),
        ('Semente de Trigo BRS 264 (40kg)', 210.00),
        ('Semente de Sorgo Granífero (20kg)', 165.00),
        ('Semente de Arroz Irrigado (40kg)', 185.00),
        ('Semente de Girassol (20kg)', 240.00),
        ('Semente de Braquiária (10kg)', 145.00),
        ('Semente de Capim Mombaça (10kg)', 155.00),
        ('Semente de Aveia Preta (25kg)', 95.00),
        ('Semente de Crotalária (25kg)', 110.00),
        ('Semente de Milheto (25kg)', 88.00),
        -- Defensivos agrícolas (31-50)
        ('Herbicida Glifosato 480 SL (1L)', 38.50),
        ('Herbicida Atrazina 500 SC (1L)', 42.90),
        ('Herbicida 2,4-D Amina (1L)', 35.00),
        ('Herbicida Paraquate (1L)', 29.90),
        ('Herbicida Cletodim (1L)', 89.00),
        ('Inseticida Clorpirifós 480 EC (1L)', 55.00),
        ('Inseticida Imidacloprido 700 WG (100g)', 65.00),
        ('Inseticida Tiametoxam 250 WG (100g)', 72.00),
        ('Inseticida Acefato 750 SP (1kg)', 48.90),
        ('Fungicida Mancozebe 800 WP (1kg)', 39.90),
        ('Fungicida Azoxistrobina 250 SC (1L)', 145.00),
        ('Fungicida Carbendazim 500 SC (1L)', 68.00),
        ('Fungicida Tebuconazol 200 EC (1L)', 79.00),
        ('Acaricida Abamectina 18 EC (1L)', 92.00),
        ('Nematicida Carbofurano (1kg)', 85.00),
        ('Adjuvante Óleo Mineral (1L)', 25.00),
        ('Espalhante Adesivo (1L)', 22.50),
        ('Inseticida Biológico Bt (1L)', 58.00),
        ('Fungicida Cúprico (1kg)', 32.00),
        ('Herbicida Pré-emergente (1L)', 95.00),
        -- Máquinas e equipamentos (51-70)
        ('Trator 75cv', 145000.00),
        ('Trator 105cv', 210000.00),
        ('Pulverizador Costal 20L', 320.00),
        ('Pulverizador de Barras 600L', 18500.00),
        ('Semeadeira 12 Linhas', 28500.00),
        ('Plantadeira 8 Linhas', 42000.00),
        ('Roçadeira a Gasolina', 1250.00),
        ('Roçadeira Lateral', 8900.00),
        ('Irrigador por Aspersão', 450.00),
        ('Pivô Central 1ha', 95000.00),
        ('Grade Aradora 18 Discos', 13500.00),
        ('Subsolador 5 Hastes', 9800.00),
        ('Arado de Disco', 7600.00),
        ('Colheitadeira de Grãos', 680000.00),
        ('Carreta Agrícola 6t', 24500.00),
        ('Distribuidor de Calcário', 18500.00),
        ('Ensiladeira', 89000.00),
        ('Triturador de Galhos', 6800.00),
        ('Motosserra 20 Polegadas', 1750.00),
        ('Roçadeira Elétrica', 380.00),
        -- Nutrição animal e pecuária (71-85)
        ('Ração Bovina Engorda (30kg)', 98.00),
        ('Ração Bovina Leiteira (30kg)', 105.00),
        ('Sal Mineral 80g (30kg)', 68.00),
        ('Sal Proteinado (30kg)', 115.00),
        ('Núcleo Mineral Suíno (20kg)', 89.00),
        ('Ração de Aves Postura (25kg)', 92.00),
        ('Ração de Aves Corte (25kg)', 88.00),
        ('Silagem de Milho (tonelada)', 320.00),
        ('Feno de Tifton (fardo)', 45.00),
        ('Brinco de Identificação (100un)', 150.00),
        ('Balança para Pecuária 1500kg', 3200.00),
        ('Comedouro para Gado', 850.00),
        ('Bebedouro Automático', 420.00),
        ('Lona para Silagem (10x15m)', 1250.00),
        ('Kit Cerca Elétrica (500m)', 1150.00),
        -- Infraestrutura e insumos (86-100)
        ('Arame Farpado 500m', 280.00),
        ('Arame Liso 1000m', 390.00),
        ('Mourão de Eucalipto (2,5m)', 22.00),
        ('Estaca Tratada 2,20m', 28.00),
        ('Tela para Galinheiro (1,5x50m)', 260.00),
        ('Bomba de Água 1,5cv', 890.00),
        ('Mangueira de Irrigação 50m', 185.00),
        ('Caixa de Água 500L', 520.00),
        ('Painel Solar Rural 550W', 980.00),
        ('Bateria Estacionária 150Ah', 1250.00),
        ('Lubrificante Agrícola 15W40 (20L)', 320.00),
        ('Óleo Hidráulico ISO 68 (20L)', 280.00),
        ('Filtro de Óleo Trator', 45.00),
        ('Correia para Trator', 120.00),
        ('Kit de Manutenção Trator', 680.00)
) AS p(nome, preco);

-- ---------------------------------------------------------------------------
-- Verificação
-- ---------------------------------------------------------------------------
SELECT
    (SELECT count(*) FROM "Branches")  AS filiais,
    (SELECT count(*) FROM "Customers") AS clientes,
    (SELECT count(*) FROM "Products")  AS produtos;
