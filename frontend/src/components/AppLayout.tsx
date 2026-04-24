import {
  AppBar,
  Avatar,
  Badge,
  Box,
  BookmarkIcon,
  BundleIcon,
  CategoryIcon,
  CommunityIcon,
  FeedIcon,
  Divider,
  Drawer,
  AdminUsersIcon,
  AIAssessmentIcon,
  AnalyticsIcon,
  HomeIcon,
  IconButton,
  KnowledgeIcon,
  LeaderboardIcon,
  LearningPathIcon,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuIcon,
  MenuItem,
  MentoringIcon,
  ModerationIcon,
  NotificationsIcon,
  ProfileIcon,
  ProposalIcon,
  RequestIcon,
  SessionIcon,
  Snackbar,
  SpeakerMarketplaceIcon,
  SpeakersIcon,
  ResumeIcon,
  ScreenerIcon,
  SurveyIcon,
  Toolbar,
  Tooltip,
  Typography,
} from '@/components/ui';
import { useCallback, useMemo, useState } from 'react';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../shared/hooks/useAuth';
import { UserRole } from '../shared/types';
import type { NotificationDto } from '../shared/types';
import { useQuery } from '@tanstack/react-query';
import { notificationsApi } from '../shared/api/notifications';
import { useNotificationHub } from '../shared/hooks/useNotificationHub';

const DRAWER_WIDTH = 240;

interface NavItem {
  label: string;
  to: string;
  icon: React.ReactNode;
  roles?: UserRole[];
}

interface NavGroup {
  heading: string;
  items: NavItem[];
}

const navGroups: NavGroup[] = [
  {
    heading: 'General',
    items: [
      { label: 'Home', to: '/', icon: <HomeIcon /> },
      { label: 'Leaderboard', to: '/leaderboard', icon: <LeaderboardIcon /> },
    ],
  },
  {
    heading: 'Knowledge',
    items: [
      { label: 'Sessions', to: '/sessions', icon: <SessionIcon /> },
      { label: 'Proposals', to: '/proposals', icon: <ProposalIcon /> },
      { label: 'Knowledge Requests', to: '/knowledge-requests', icon: <RequestIcon /> },
      { label: 'Knowledge Assets', to: '/knowledge-assets', icon: <KnowledgeIcon /> },
      { label: 'Knowledge Bundles', to: '/knowledge-bundles', icon: <BundleIcon /> },
      { label: 'Learning Paths', to: '/learning-paths', icon: <LearningPathIcon /> },
    ],
  },
  {
    heading: 'Community',
    items: [
      { label: 'Feed', to: '/feed', icon: <FeedIcon /> },
      { label: 'Bookmarks', to: '/bookmarks', icon: <BookmarkIcon /> },
      { label: 'Communities', to: '/communities', icon: <CommunityIcon /> },
      { label: 'Speakers', to: '/speakers', icon: <SpeakersIcon /> },
      { label: 'Speaker Marketplace', to: '/speaker-marketplace', icon: <SpeakerMarketplaceIcon /> },
      { label: 'Mentoring', to: '/mentoring', icon: <MentoringIcon /> },
    ],
  },
  {
    heading: 'Talent',
    items: [
      { label: 'Resume Builder', to: '/talent/resume-builder', icon: <ResumeIcon /> },
      {
        label: 'Resume Screener',
        to: '/talent/screening',
        icon: <ScreenerIcon />,
        roles: [UserRole.Admin, UserRole.SuperAdmin, UserRole.Manager, UserRole.KnowledgeTeam],
      },
      {
        label: 'Assessment',
        to: '/assessment',
        icon: <AIAssessmentIcon />,
        roles: [UserRole.Admin, UserRole.SuperAdmin],
      },
    ],
  },
  {
    heading: 'Administration',
    items: [
      {
        label: 'Analytics',
        to: '/analytics',
        icon: <AnalyticsIcon />,
        roles: [UserRole.Admin, UserRole.KnowledgeTeam, UserRole.SuperAdmin, UserRole.Manager],
      },
      {
        label: 'Categories',
        to: '/categories',
        icon: <CategoryIcon />,
        roles: [UserRole.Admin, UserRole.KnowledgeTeam, UserRole.SuperAdmin],
      },
      {
        label: 'Tags',
        to: '/tags',
        icon: <KnowledgeIcon />,
        roles: [UserRole.Admin, UserRole.KnowledgeTeam, UserRole.SuperAdmin],
      },
      {
        label: 'Users',
        to: '/admin/users',
        icon: <AdminUsersIcon />,
        roles: [UserRole.Admin, UserRole.SuperAdmin],
      },
      {
        label: 'Moderation',
        to: '/admin/moderation',
        icon: <ModerationIcon />,
        roles: [UserRole.Admin, UserRole.SuperAdmin],
      },
      {
        label: 'Surveys',
        to: '/admin/surveys',
        icon: <SurveyIcon />,
        roles: [UserRole.Admin, UserRole.SuperAdmin],
      },
    ],
  },
];

export function AppLayout() {
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const handleNotification = useCallback((n: NotificationDto) => {
    setToastMessage(n.title);
  }, []);
  useNotificationHub(handleNotification);

  const { data: notifications } = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: () => notificationsApi.getNotifications({ isRead: false, pageSize: 1 }),
    refetchInterval: 60_000,
  });

  const unreadCount = notifications?.totalCount ?? 0;

  // FE-11: memoize visibleGroups — AppLayout re-renders every 60 s due to the notification
  // poll updating unreadCount; recomputing role-filtered nav items on each render is wasteful
  const visibleGroups = useMemo(
    () =>
      navGroups
        .map((group) => ({
          ...group,
          items: group.items.filter(
            (item) => !item.roles || item.roles.some((r) => hasRole(r))
          ),
        }))
        .filter((group) => group.items.length > 0),
    [hasRole]
  );

  // Shared drawer contents (used by both temporary and permanent drawers)
  const drawerContent = (
    <>
      <Toolbar>
        <Typography variant="h6" noWrap fontWeight={700} color="primary">
          KnowHub
        </Typography>
      </Toolbar>
      <Divider />
      <Box sx={{ overflowY: 'auto', flex: 1 }}>
        {visibleGroups.map((group, gi) => (
          <Box key={group.heading}>
            {gi > 0 && <Divider sx={{ my: 0.5 }} />}
            <Typography
              variant="caption"
              sx={{
                px: 2,
                pt: 1.5,
                pb: 0.5,
                display: 'block',
                color: 'text.disabled',
                fontWeight: 600,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
              }}
            >
              {group.heading}
            </Typography>
            <List disablePadding>
              {group.items.map((item) => (
                <ListItemButton
                  key={item.to}
                  component={Link}
                  to={item.to}
                  onClick={() => setMobileOpen(false)}
                  sx={{ py: 0.75 }}
                >
                  <ListItemIcon sx={{ minWidth: 38 }}>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} primaryTypographyProps={{ variant: 'body2' }} />
                </ListItemButton>
              ))}
            </List>
          </Box>
        ))}
      </Box>
    </>
  );

  return (
    <>
      <Box sx={{ display: 'flex' }}>
        {/* ── Mobile: temporary slide-in drawer ────────────────────── */}
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }} // better mobile perf
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
          }}
        >
          {drawerContent}
        </Drawer>

        {/* ── Desktop: permanent sidebar ────────────────────────────── */}
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            width: DRAWER_WIDTH,
            flexShrink: 0,
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
          }}
        >
          {drawerContent}
        </Drawer>

        {/* ── Main area ─────────────────────────────────────────────── */}
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            display: 'flex',
            flexDirection: 'column',
            // On desktop the sidebar is in the normal flow; on mobile it overlays
            width: { xs: '100%', md: `calc(100% - ${DRAWER_WIDTH}px)` },
            minWidth: 0,
          }}
        >
          {/* Top AppBar */}
          <AppBar
            position="sticky"
            elevation={0}
            sx={{ borderBottom: '1px solid', borderColor: 'divider', bgcolor: 'background.paper' }}
          >
            <Toolbar>
              {/* Hamburger — visible only on mobile */}
              <IconButton
                aria-label="open navigation"
                edge="start"
                onClick={() => setMobileOpen(true)}
                sx={{ mr: 1, display: { md: 'none' }, color: 'text.secondary' }}
              >
                <MenuIcon />
              </IconButton>

              {/* App name shown on mobile (no sidebar visible) */}
              <Typography
                variant="h6"
                fontWeight={700}
                color="primary"
                noWrap
                sx={{ display: { xs: 'block', md: 'none' }, flexGrow: 1 }}
              >
                KnowHub
              </Typography>

              <Box flexGrow={1} sx={{ display: { xs: 'none', md: 'block' } }} />

              {/* Notifications */}
              <Tooltip title="Notifications">
                <IconButton component={Link} to="/notifications" sx={{ mr: 1, color: 'text.secondary' }}>
                  <Badge badgeContent={unreadCount > 0 ? unreadCount : undefined} color="error">
                    <NotificationsIcon />
                  </Badge>
                </IconButton>
              </Tooltip>

              {/* Avatar / profile menu */}
              <Tooltip title={user?.fullName ?? ''}>
                <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} size="small">
                  <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}>
                    {user?.fullName?.[0] ?? '?'}
                  </Avatar>
                </IconButton>
              </Tooltip>

              <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={() => setAnchorEl(null)}
                transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
              >
                <MenuItem
                  onClick={() => {
                    setAnchorEl(null);
                    navigate(`/profile/${user?.userId}`);
                  }}
                >
                  <ListItemIcon>
                    <ProfileIcon fontSize="small" />
                  </ListItemIcon>
                  My Profile
                </MenuItem>
                <Divider />
                <MenuItem
                  onClick={() => {
                    setAnchorEl(null);
                    logout();
                    navigate('/login');
                  }}
                >
                  Sign out
                </MenuItem>
              </Menu>
            </Toolbar>
          </AppBar>

          {/* Page content */}
          <Box
            sx={{
              p: { xs: 2, sm: 3 },
              flexGrow: 1,
              bgcolor: 'background.default',
              minHeight: '100%',
              overflowX: 'hidden',
            }}
          >
            <Outlet />
          </Box>
        </Box>
      </Box>

      {/* Real-time notification toast */}
      <Snackbar
        open={Boolean(toastMessage)}
        autoHideDuration={5000}
        onClose={() => setToastMessage(null)}
        message={toastMessage}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      />
    </>
  );
}
