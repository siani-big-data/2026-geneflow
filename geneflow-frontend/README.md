<div align="center">

```
 ██████╗ ███████╗███╗   ██╗███████╗███████╗██╗      ██████╗ ██╗    ██╗
██╔════╝ ██╔════╝████╗  ██║██╔════╝██╔════╝██║     ██╔═══██╗██║    ██║
██║  ███╗█████╗  ██╔██╗ ██║█████╗  █████╗  ██║     ██║   ██║██║ █╗ ██║
██║   ██║██╔══╝  ██║╚██╗██║██╔══╝  ██╔══╝  ██║     ██║   ██║██║███╗██║
╚██████╔╝███████╗██║ ╚████║███████╗██║     ███████╗╚██████╔╝╚███╔███╔╝
 ╚═════╝ ╚══════╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝
        ███████╗██████╗  ██████╗ ███╗   ██╗████████╗███████╗███╗   ██╗██████╗
        ██╔════╝██╔══██╗██╔═══██╗████╗  ██║╚══██╔══╝██╔════╝████╗  ██║██╔══██╗
        █████╗  ██████╔╝██║   ██║██╔██╗ ██║   ██║   █████╗  ██╔██╗ ██║██║  ██║
        ██╔══╝  ██╔══██╗██║   ██║██║╚██╗██║   ██║   ██╔══╝  ██║╚██╗██║██║  ██║
        ██║     ██║  ██║╚██████╔╝██║ ╚████║   ██║   ███████╗██║ ╚████║██████╔╝
        ╚═╝     ╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═══╝   ╚═╝   ╚══════╝╚═╝  ╚═══╝╚═════╝
```

**Modern Bioinformatics Platform for the GeneFlow Ecosystem**

[![Next.js](https://img.shields.io/badge/Next.js-16.2.1-000000?logo=next.js&logoColor=white)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-19-61dafb?logo=react&logoColor=white)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178c6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-v4-06b6d4?logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red)]()

</div>

---

GeneFlow Frontend is the **web interface** for the GeneFlow platform — a SaaS bioinformatics solution for genomic study management, trace visualization, pipeline orchestration, and real-time analysis dashboards.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           GeneFlow Frontend                              │
├─────────────────────────────────────────────────────────────────────────┤
│  Dashboard  │  Studies  │  Traces  │  Pipelines  │  Analysis  │  Discover│
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │
│   │   Studies    │  │    Traces    │  │  Pipelines   │                  │
│   │  Management  │  │   Viewer     │  │  Execution   │                  │
│   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘                  │
│          │                 │                 │                           │
│          └─────────────────┴─────────────────┘                           │
│                            │                                             │
│                            ▼                                             │
│                    ┌───────────────┐                                     │
│                    │  GeneFlow API │                                     │
│                    └───────────────┘                                     │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Features

| Feature | Description |
|---------|-------------|
| **Dashboard** | Real-time metrics, activity feed, and quick actions |
| **Studies** | Create, organize, and manage genomic research projects |
| **Traces** | Interactive visualization of genetic sequences (AB1, FASTA) |
| **Pipelines** | Execute and monitor bioinformatics workflows |
| **Analysis** | Charts, statistics, and data exploration tools |
| **Discover** | Search and explore public datasets |
| **Billing** | Subscription management and usage tracking |

---

## Quick Start

```bash
# Clone the repository
git clone https://github.com/geneflow-app/geneflow-frontend.git
cd geneflow-frontend

# Install dependencies
npm install

# Run development server
npm run dev

# Open http://localhost:3000
```

---

## Tech Stack

| Category | Technology |
|----------|------------|
| **Framework** | Next.js 16.2.1 (App Router) |
| **UI Library** | React 19 |
| **Language** | TypeScript 5 |
| **Styling** | Tailwind CSS v4 |
| **Components** | Radix UI Primitives |
| **State** | Zustand + TanStack Query |
| **Forms** | React Hook Form + Zod |
| **Charts** | Recharts |
| **Icons** | Lucide React |

---

## Project Structure

```
geneflow-frontend/
├── app/                                 # Next.js App Router
│   ├── (auth)/                          # Public routes (login, register)
│   │   ├── login/page.tsx
│   │   ├── register/page.tsx
│   │   └── layout.tsx
│   ├── (platform)/                      # Protected routes
│   │   ├── layout.tsx                   # Platform layout (sidebar + header)
│   │   ├── dashboard/page.tsx
│   │   ├── discover/page.tsx
│   │   ├── studies/
│   │   │   ├── page.tsx
│   │   │   └── [studyId]/page.tsx
│   │   ├── traces/
│   │   │   ├── page.tsx
│   │   │   └── [traceId]/page.tsx
│   │   ├── pipelines/page.tsx
│   │   ├── analysis/page.tsx
│   │   ├── profile/page.tsx
│   │   ├── settings/
│   │   │   ├── page.tsx
│   │   │   └── billing/page.tsx
│   │   └── help/page.tsx
│   ├── layout.tsx                       # Root layout
│   ├── globals.css
│   └── not-found.tsx
│
├── src/
│   ├── components/
│   │   ├── ui/                          # UI primitives (Radix-based)
│   │   ├── layout/                      # Sidebar, Header, Navigation
│   │   ├── features/                    # Feature-specific components
│   │   └── shared/                      # Shared components
│   │
│   ├── lib/                             # Utilities (cn, formatters)
│   ├── hooks/                           # Custom React hooks
│   ├── services/                        # API service layer
│   ├── types/                           # TypeScript definitions
│   ├── stores/                          # Zustand stores
│   ├── providers/                       # Context providers
│   └── mocks/                           # Mock data for development
│
└── docs/                                # Design system documentation
```

---

## Scripts

```bash
npm run dev       # Start development server
npm run build     # Build for production
npm run start     # Start production server
npm run lint      # Run ESLint
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXT_PUBLIC_API_URL` | GeneFlow API base URL | `http://localhost:5000` |
| `NEXT_PUBLIC_APP_URL` | Frontend application URL | `http://localhost:3000` |

---

## Architecture

### Route Groups

| Group | Path | Description |
|-------|------|-------------|
| `(auth)` | `/login`, `/register` | Public authentication routes |
| `(platform)` | `/dashboard`, `/studies`, etc. | Protected platform routes |

### State Management

```
┌─────────────────────────────────────────────────────────────────┐
│                        State Architecture                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌─────────────────┐     ┌─────────────────┐                   │
│   │  TanStack Query │     │     Zustand     │                   │
│   │  (Server State) │     │  (Client State) │                   │
│   ├─────────────────┤     ├─────────────────┤                   │
│   │ • Studies       │     │ • UI State      │                   │
│   │ • Traces        │     │ • Sidebar       │                   │
│   │ • Pipelines     │     │ • Theme         │                   │
│   │ • User Data     │     │ • Filters       │                   │
│   └─────────────────┘     └─────────────────┘                   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Component Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│  Providers (Theme, Query, Toast)                                 │
│  └─► Root Layout                                                 │
│      └─► Platform Layout (Sidebar + Header)                      │
│          └─► Page Content                                        │
│              ├─► Feature Components                              │
│              │   └─► UI Primitives (Radix)                       │
│              └─► Shared Components                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Next.js 16 Notes

This project uses Next.js 16 with **async params** (breaking change from v15):

```typescript
// Page with dynamic route params
export default async function StudyPage({
  params,
}: {
  params: Promise<{ studyId: string }>
}) {
  const { studyId } = await params;
  // ...
}

// Page with search params
export default async function StudiesPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; filter?: string }>
}) {
  const { page, filter } = await searchParams;
  // ...
}
```

---

## Development

### Prerequisites

- Node.js 20+
- npm 10+

### Code Style

- ESLint + Prettier for formatting
- Conventional Commits for git messages
- TypeScript strict mode enabled

### Branch Strategy

```
main
└── develop
    ├── feature/foundation
    ├── feature/layout
    ├── feature/core-pages
    ├── feature/pipelines-analysis
    ├── feature/user-settings
    └── feature/polish
```

---

## Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Foundation (types, UI components, providers) | ⏳ In Progress |
| 2 | Layout & Navigation (sidebar, header) | ⏳ Pending |
| 3 | Core Pages (dashboard, studies, traces) | ⏳ Pending |
| 4 | Pipelines & Analysis | ⏳ Pending |
| 5 | User & Settings | ⏳ Pending |
| 6 | Polish & Forms | ⏳ Pending |

---

## Related Projects

| Project | Description |
|---------|-------------|
| [GeneFlow API](https://github.com/geneflow-app/GeneFlow) | .NET Core backend API |
| [GeneFlow Datalake](https://github.com/geneflow-app/GeneFlow-Datalake) | Event store and data lake |
| [GeneFlow AI](https://github.com/geneflow-app/GeneFlow-AI) | AI-powered sequence analysis |

---

<div align="center">

**GeneFlow Platform** · Proprietary

</div>
