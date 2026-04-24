import { lazy, Suspense } from 'react';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { PrivateRoute } from './components/PrivateRoute';
import { AppLayout } from './components/AppLayout';
import { LoadingOverlay } from './shared/components/LoadingOverlay';
import { UserRole } from './shared/types';

// Lazy-loaded pages
const LoginPage = lazy(() => import('./features/auth/LoginPage'));
const RegisterPage = lazy(() => import('./features/auth/RegisterPage'));
const SessionListPage = lazy(() => import('./features/sessions/SessionListPage'));
const SessionDetailPage = lazy(() => import('./features/sessions/SessionDetailPage'));
const SessionFormPage = lazy(() => import('./features/sessions/SessionFormPage'));
const ProposalListPage = lazy(() => import('./features/proposals/ProposalListPage'));
const ProposalDetailPage = lazy(() => import('./features/proposals/ProposalDetailPage'));
const ProposalFormPage = lazy(() => import('./features/proposals/ProposalFormPage'));
const CommunityListPage = lazy(() => import('./features/communities/CommunityListPage'));
const CommunityDetailPage = lazy(() => import('./features/communities/CommunityDetailPage'));
const CommunityFormPage = lazy(() => import('./features/communities/CommunityFormPage'));
const PostDetailPage = lazy(() => import('./features/communities/PostDetailPage'));
const PostEditorPage = lazy(() => import('./features/communities/PostEditorPage'));
const SpeakerDirectoryPage = lazy(() => import('./features/speakers/SpeakerDirectoryPage'));
const SpeakerDetailPage = lazy(() => import('./features/speakers/SpeakerDetailPage'));
const KnowledgeRequestsPage = lazy(() => import('./features/knowledge-requests/KnowledgeRequestsPage'));
const NotificationsPage = lazy(() => import('./features/notifications/NotificationsPage'));
const UserProfilePage = lazy(() => import('./features/profile/UserProfilePage'));
const DashboardPage = lazy(() => import('./features/dashboard/DashboardPage'));
const CategoriesPage = lazy(() => import('./features/categories/CategoriesPage'));
const TagsPage = lazy(() => import('./features/tags/TagsPage'));

// Admin pages
const UserManagementPage = lazy(() => import('./features/admin/UserManagementPage'));
const ModerationPage = lazy(() => import('./features/admin/ModerationPage'));

// Phase 2 lazy pages
const LearningPathListPage = lazy(() => import('./features/learning-paths/LearningPathListPage'));
const LearningPathDetailPage = lazy(() => import('./features/learning-paths/LearningPathDetailPage'));
const LearningPathFormPage = lazy(() => import('./features/learning-paths/LearningPathFormPage'));
const KnowledgeAssetListPage = lazy(() => import('./features/knowledge-assets/KnowledgeAssetListPage'));
const KnowledgeAssetFormPage = lazy(() => import('./features/knowledge-assets/KnowledgeAssetFormPage'));
const KnowledgeAssetDetailPage = lazy(() => import('./features/knowledge-assets/KnowledgeAssetDetailPage'));
const LeaderboardPage = lazy(() => import('./features/leaderboards/LeaderboardPage'));
const MentoringPage = lazy(() => import('./features/mentoring/MentoringPage'));
const KnowledgeBundleListPage = lazy(() => import('./features/knowledge-bundles/KnowledgeBundleListPage'));
const KnowledgeBundleDetailPage = lazy(() => import('./features/knowledge-bundles/KnowledgeBundleDetailPage'));

// Phase 3 lazy pages
const AnalyticsDashboardPage = lazy(() => import('./features/analytics/AnalyticsDashboardPage'));
const SpeakerMarketplacePage = lazy(() => import('./features/speaker-marketplace/SpeakerMarketplacePage'));

// AI Assessment module
const AssessmentPage = lazy(() => import('./features/assessment/AssessmentPage'));

// Survey module
const SurveysPage = lazy(() => import('./features/surveys/SurveysPage'));
const SurveyBuilderPage = lazy(() => import('./features/surveys/SurveyBuilderPage'));
const SurveyComparePage = lazy(() => import('./features/surveys/SurveyComparePage'));
const SurveyFormPage = lazy(() => import('./features/surveys/SurveyFormPage'));

// Talent module
const ResumeBuilderPage = lazy(() => import('./features/talent/resume-builder/ResumeBuilderPage'));
const ScreeningListPage = lazy(() => import('./features/talent/screening/ScreeningListPage'));
const ScreeningDetailPage = lazy(() => import('./features/talent/screening/ScreeningDetailPage'));

// Dev Community Feed module
const FeedPage = lazy(() => import('./features/feed/FeedPage'));
const BookmarksPage = lazy(() => import('./features/feed/BookmarksPage'));
const SeriesDetailPage = lazy(() => import('./features/communities/SeriesDetailPage'));

const router = createBrowserRouter([
  {
    path: '/login',
    element: (
      <Suspense fallback={<LoadingOverlay fullPage />}>
        <LoginPage />
      </Suspense>
    ),
  },
  {
    path: '/register',
    element: (
      <Suspense fallback={<LoadingOverlay fullPage />}>
        <RegisterPage />
      </Suspense>
    ),
  },
  // Public survey form — no auth, no AppLayout
  {
    path: '/survey/:token',
    element: (
      <Suspense fallback={<LoadingOverlay fullPage />}>
        <SurveyFormPage />
      </Suspense>
    ),
  },
  {
    element: <PrivateRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          {
            path: '/',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <DashboardPage />
              </Suspense>
            ),
          },
          // Sessions
          {
            path: '/sessions',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SessionListPage />
              </Suspense>
            ),
          },
          {
            path: '/sessions/new',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SessionFormPage />
              </Suspense>
            ),
          },
          {
            path: '/sessions/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SessionDetailPage />
              </Suspense>
            ),
          },
          {
            path: '/sessions/:id/edit',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SessionFormPage />
              </Suspense>
            ),
          },
          // Proposals
          {
            path: '/proposals',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <ProposalListPage />
              </Suspense>
            ),
          },
          {
            path: '/proposals/new',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <ProposalFormPage />
              </Suspense>
            ),
          },
          {
            path: '/proposals/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <ProposalDetailPage />
              </Suspense>
            ),
          },
          {
            path: '/proposals/:id/edit',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <ProposalFormPage />
              </Suspense>
            ),
          },
          // Communities
          {
            path: '/communities',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <CommunityListPage />
              </Suspense>
            ),
          },
          {
            path: '/communities/new',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <CommunityFormPage />
              </Suspense>
            ),
          },
          {
            path: '/communities/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <CommunityDetailPage />
              </Suspense>
            ),
          },
          {
            path: '/communities/:id/edit',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <CommunityFormPage />
              </Suspense>
            ),
          },
          // Community Posts
          {
            path: '/communities/:id/posts/new',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <PostEditorPage />
              </Suspense>
            ),
          },
          {
            path: '/communities/:id/posts/:postId',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <PostDetailPage />
              </Suspense>
            ),
          },
          {
            path: '/communities/:id/posts/:postId/edit',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <PostEditorPage />
              </Suspense>
            ),
          },
          // Series
          {
            path: '/communities/:id/series/:seriesId',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SeriesDetailPage />
              </Suspense>
            ),
          },
          // Feed & Bookmarks
          {
            path: '/feed',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <FeedPage />
              </Suspense>
            ),
          },
          {
            path: '/bookmarks',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <BookmarksPage />
              </Suspense>
            ),
          },
          // Speakers
          {
            path: '/speakers',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SpeakerDirectoryPage />
              </Suspense>
            ),
          },
          {
            path: '/speakers/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SpeakerDetailPage />
              </Suspense>
            ),
          },
          // Knowledge Requests
          {
            path: '/knowledge-requests',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <KnowledgeRequestsPage />
              </Suspense>
            ),
          },
          // Notifications
          {
            path: '/notifications',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <NotificationsPage />
              </Suspense>
            ),
          },
          // Profile
          {
            path: '/profile/:userId',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <UserProfilePage />
              </Suspense>
            ),
          },
          // Categories — KnowledgeTeam+
          {
            element: <PrivateRoute requiredRoles={[UserRole.KnowledgeTeam, UserRole.Admin, UserRole.SuperAdmin]} />,
            children: [
              {
                path: '/categories',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <CategoriesPage />
                  </Suspense>
                ),
              },
              // Tags
              {
                path: '/tags',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <TagsPage />
                  </Suspense>
                ),
              },
            ],
          },
          // Phase 2 — Learning Paths
          {
            path: '/learning-paths',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <LearningPathListPage />
              </Suspense>
            ),
          },
          {
            path: '/learning-paths/new',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <LearningPathFormPage />
              </Suspense>
            ),
          },
          {
            path: '/learning-paths/:id/edit',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <LearningPathFormPage />
              </Suspense>
            ),
          },
          {
            path: '/learning-paths/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <LearningPathDetailPage />
              </Suspense>
            ),
          },
          // Phase 2 — Knowledge Assets
          {
            path: '/knowledge-assets',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <KnowledgeAssetListPage />
              </Suspense>
            ),
          },
          {
            path: '/knowledge-assets/new',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <KnowledgeAssetFormPage />
              </Suspense>
            ),
          },
          {
            path: '/knowledge-assets/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <KnowledgeAssetDetailPage />
              </Suspense>
            ),
          },
          // Phase 2 — Leaderboards
          {
            path: '/leaderboard',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <LeaderboardPage />
              </Suspense>
            ),
          },
          // Phase 2 — Mentoring
          {
            path: '/mentoring',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <MentoringPage />
              </Suspense>
            ),
          },
          // Phase 2 — Knowledge Bundles
          {
            path: '/knowledge-bundles',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <KnowledgeBundleListPage />
              </Suspense>
            ),
          },
          {
            path: '/knowledge-bundles/:id',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <KnowledgeBundleDetailPage />
              </Suspense>
            ),
          },
          // Admin — User Management & Moderation (Admin/SuperAdmin only)
          {
            element: <PrivateRoute requiredRoles={[UserRole.Admin, UserRole.SuperAdmin]} />,
            children: [
              {
                path: '/admin/users',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <UserManagementPage />
                  </Suspense>
                ),
              },
              {
                path: '/admin/moderation',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <ModerationPage />
                  </Suspense>
                ),
              },
              // Surveys
              {
                path: '/admin/surveys',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <SurveysPage />
                  </Suspense>
                ),
              },
              {
                path: '/admin/surveys/compare',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <SurveyComparePage />
                  </Suspense>
                ),
              },
              {
                path: '/admin/surveys/:id',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <SurveyBuilderPage />
                  </Suspense>
                ),
              },
            ],
          },
          // Phase 3 — Analytics (Manager+)
          {
            element: <PrivateRoute requiredRoles={[UserRole.Manager, UserRole.KnowledgeTeam, UserRole.Admin, UserRole.SuperAdmin]} />,
            children: [
              {
                path: '/analytics',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <AnalyticsDashboardPage />
                  </Suspense>
                ),
              },
            ],
          },
          // Phase 3 — Speaker Marketplace
          {
            path: '/speaker-marketplace',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <SpeakerMarketplacePage />
              </Suspense>
            ),
          },
          // AI Assessment Module (Admin+)
          {
            element: <PrivateRoute requiredRoles={[UserRole.Admin, UserRole.SuperAdmin]} />,
            children: [
              {
                path: '/assessment',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <AssessmentPage />
                  </Suspense>
                ),
              },
            ],
          },
          // Talent Module — Resume Builder (all users)
          {
            path: '/talent/resume-builder',
            element: (
              <Suspense fallback={<LoadingOverlay />}>
                <ResumeBuilderPage />
              </Suspense>
            ),
          },
          // Talent Module — Screening (Manager/KnowledgeTeam/Admin+)
          {
            element: <PrivateRoute requiredRoles={[UserRole.Manager, UserRole.KnowledgeTeam, UserRole.Admin, UserRole.SuperAdmin]} />,
            children: [
              {
                path: '/talent/screening',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <ScreeningListPage />
                  </Suspense>
                ),
              },
              {
                path: '/talent/screening/:jobId',
                element: (
                  <Suspense fallback={<LoadingOverlay />}>
                    <ScreeningDetailPage />
                  </Suspense>
                ),
              },
            ],
          },
        ],
      },
    ],
  },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
