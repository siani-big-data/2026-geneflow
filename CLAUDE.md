@AGENTS.md

# GeneFlow Frontend

## Proyecto
Plataforma SaaS de bioinformática para gestión de estudios genómicos, visualización de trazas y orquestación de pipelines.

## Stack
- Next.js 16.2.1 (App Router, async params obligatorio)
- React 19
- TypeScript 5
- Tailwind CSS v4
- Radix UI + class-variance-authority
- TanStack Query + Zustand
- Recharts para gráficos
- Sonner para toast notifications
- Zod para validación

## Estructura
```
app/
├── (auth)/             # Login, Register, Forgot Password
├── (platform)/         # Rutas protegidas con sidebar+header
├── components/
│   ├── ui/             # Primitivos (Button, Card, Dialog, Toaster...)
│   ├── layout/         # Sidebar, Header, PageHeader
│   └── shared/         # StatusBadge, EmptyState, SkipLink...
├── hooks/              # useStudies, useTraces, usePipelines, useFocusTrap...
├── lib/                # utils (cn), constants, validations/
├── mocks/              # Datos mock y API simulada
├── providers/          # ThemeProvider, QueryProvider
├── services/           # Capa de servicios (mock por ahora)
├── stores/             # Zustand (ui-store)
├── types/              # TypeScript definitions
├── globals.css
├── layout.tsx
└── page.tsx            # Landing page
```

## Convenciones Git
- Ramas: `feat/x`, `fix/x`, `chore/x`
- Commits: `feat(scope): message` - una línea, sin coautor
- NO commitear: `.claude/`, `docs/`, `IMPLEMENTATION_PLAN.md`

## Design System
- Colores principales: `teal` (#0d9488), `blue-deep` (#1e40af)
- Los tokens están en `app/globals.css`
- Referencia visual en `docs/GeneFlow Bioinformatics Interface/`

## Layout
- Platform layout: `px-16 py-10` (ancho completo, sin max-width)
- Sidebar colapsable (64px collapsed, 256px expanded)
- Auth layout: split-screen con imagen lateral

## Fases Implementadas
- [x] Fase 1: Foundation (types, UI components, providers)
- [x] Fase 2: Layout & Navigation (sidebar, header, rutas)
- [x] Fase 3: Mock Data & Core Pages (dashboard, studies, traces)
- [x] Fase 4: Pipelines (con métricas y charts)
- [x] Fase 5: User & Settings (settings, profile, help)
- [x] Fase 6: Polish & Forms (landing, auth, modals, skeletons, a11y)

## Páginas Implementadas
- `/` - Landing page con 3D hero, features, pricing
- `/login` - Login con 2FA, OAuth (Google, GitHub)
- `/register` - Registro con validación de password
- `/forgot-password` - Recuperar contraseña
- `/dashboard` - Overview con stats y charts
- `/studies` - Lista de estudios con filtros
- `/studies/[id]` - Detalle de estudio
- `/traces` - Grid de trazas
- `/traces/[id]` - Visor de chromatogram
- `/pipelines` - Pipelines con métricas
- `/settings` - Configuración con tabs
- `/profile` - Perfil de usuario
- `/help` - Centro de ayuda
- `/discover` - Explorar estudios públicos

## Componentes Destacados
- `Chromatogram` - Visor canvas de secuencias (performance optimizado)
- `CreateStudyModal` / `CreatePipelineModal` - Modales funcionales
- `ErrorBoundary` - Manejo de errores global
- `SkipLink` - Accesibilidad
- `Toaster` - Notificaciones toast

## Ramas de Features (sin mergear)
- `feat/toast-notifications` - Sonner toasts
- `feat/loading-skeletons` - Loading states
- `feat/modals` - Create Study/Pipeline modals
- `feat/error-boundary` - Error handling
- `feat/form-validation` - Zod schemas
- `feat/accessibility` - Skip link, focus trap

## Notas Next.js 16
```typescript
// Params asíncronos obligatorios
export default async function Page({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params;
}
```

## Comandos
```bash
npm run dev      # Puerto 3000
npm run build    # Verificar compilación
npm run start    # Producción
```

## Imágenes en public/
- `hero-side.png` - Imagen para auth layout
- `dashboard-light.png` / `dashboard-dark.png` - Screenshots para landing
