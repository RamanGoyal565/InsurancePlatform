import { useNavigate } from 'react-router-dom';
import { Box, Button, CircularProgress, Container, Divider, IconButton, Skeleton, Typography } from '@mui/material';
import MailOutlineIcon from '@mui/icons-material/MailOutlined';
import PublicIcon from '@mui/icons-material/Public';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import TwoWheelerIcon from '@mui/icons-material/TwoWheeler';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import { usePolicies } from '../../hooks/usePolicies';
import BrandLogo from '../../components/ui/BrandLogo';

// ── Design tokens from DESIGN.md ──────────────────────────────────────────────
const T = {
  primary:        '#002045',
  primaryContainer: '#1a365d',
  onPrimaryContainer: '#86a0cd',
  secondary:      '#545f72',
  secondaryContainer: '#d5e0f7',
  surface:        '#faf9fd',
  surfaceContainerLow: '#f4f3f7',
  surfaceContainer: '#efedf1',
  surfaceContainerHigh: '#e9e7eb',
  onSurface:      '#1a1c1e',
  onSurfaceVariant: '#43474e',
  outline:        '#74777f',
  outlineVariant: '#c4c6cf',
  // Safety Orange — primary CTA only
  cta:            '#e65100',
  ctaHover:       '#bf360c',
  success:        '#1b5e20',
  successBg:      '#e8f5e9',
  warning:        '#e65100',
  warningBg:      '#fff3e0',
  error:          '#ba1a1a',
  errorBg:        '#ffdad6',
};

// ── Sample featured policies ──────────────────────────────────────────────────
// Removed — now fetched from API

const VEHICLE_ICONS = { 1: <DirectionsCarIcon sx={{ fontSize: 16, color: T.onSurfaceVariant }} />, 2: <LocalShippingIcon sx={{ fontSize: 16, color: T.onSurfaceVariant }} />, 3: <TwoWheelerIcon sx={{ fontSize: 16, color: T.onSurfaceVariant }} /> };
const VEHICLE_LABELS = { 1: 'Car', 2: 'Truck', 3: 'Bike' };

/** Split coverage — semicolon-separated only: "Item one;Item two;Item three" */
const splitCoverage = (str) =>
  str ? str.split(';').map((s) => s.trim()).filter(Boolean) : [];

function StatusChip({ status }) {
  const map = {
    Active:  { bg: T.successBg,  color: T.success,  label: 'ACTIVE' },
    Pending: { bg: T.warningBg,  color: T.warning,  label: 'PENDING' },
    Expired: { bg: T.errorBg,    color: T.error,    label: 'EXPIRED' },
  };
  const s = map[status] || map.Active;
  return (
    <Box
      sx={{
        display: 'inline-flex', alignItems: 'center',
        px: 1.25, py: 0.25,
        borderRadius: '9999px',
        bgcolor: s.bg,
        color: s.color,
        fontSize: 11,
        fontWeight: 700,
        letterSpacing: '0.06em',
        fontFamily: 'Inter, sans-serif',
      }}
    >
      {s.label}
    </Box>
  );
}

// ── Reusable section heading ──────────────────────────────────────────────────
function SectionHeading({ children }) {
  return (
    <Typography
      sx={{
        fontFamily: 'Inter, sans-serif',
        fontSize: 22,
        fontWeight: 600,
        color: T.onSurface,
        lineHeight: 1.3,
        mb: 0.5,
      }}
    >
      {children}
    </Typography>
  );
}

export default function LandingPage() {
  const navigate = useNavigate();
  const { data: policies = [], isLoading: policiesLoading } = usePolicies();

  // Show up to 6 policies in the featured table
  const featuredPolicies = policies.slice(0, 6);

  return (
    <Box sx={{ bgcolor: T.surface, minHeight: '100vh', fontFamily: 'Inter, sans-serif' }}>

      {/* ── NAV ─────────────────────────────────────────────────────────────── */}
      <Box
        component="nav"
        sx={{
          bgcolor: T.primary,
          px: { xs: 2.5, md: 4 },
          py: 1.75,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          position: 'sticky',
          top: 0,
          zIndex: 100,
        }}
      >
        {/* Logo */}
        <BrandLogo size="md" />

        {/* Nav links — desktop */}
        <Box sx={{ display: { xs: 'none', md: 'flex' }, gap: 3.5 }}>
          {[
            { label: 'Home', path: '/' },
            { label: 'Policies', path: '/policies' },
            { label: 'Claims', path: '/login' },
            //{ label: 'About Us', path: '/' },
          ].map((item) => (
            <Typography
              key={item.label}
              onClick={() => navigate(item.path)}
              sx={{
                color: 'rgba(255,255,255,0.75)',
                fontSize: 14,
                fontWeight: 500,
                cursor: 'pointer',
                '&:hover': { color: '#fff' },
                transition: 'color 0.15s',
              }}
            >
              {item.label}
            </Typography>
          ))}
        </Box>

        {/* CTA */}
        <Button
          onClick={() => navigate('/login')}
          sx={{
            bgcolor: 'transparent',
            border: '1px solid rgba(255,255,255,0.35)',
            color: '#fff',
            fontSize: 13,
            fontWeight: 600,
            px: 2.5,
            py: 0.75,
            borderRadius: '4px',
            textTransform: 'none',
            '&:hover': { bgcolor: 'rgba(255,255,255,0.08)', borderColor: 'rgba(255,255,255,0.6)' },
          }}
        >
          Log In
        </Button>
      </Box>

      {/* ── HERO ────────────────────────────────────────────────────────────── */}
      <Box
        sx={{
          position: 'relative',
          px: { xs: 2.5, md: 8 },
          pt: { xs: 7, md: 10 },
          pb: { xs: 8, md: 12 },
          overflow: 'hidden',
          // Car image as background
          backgroundImage: `url('/car-hero.jpg')`,
          backgroundSize: 'cover',
          backgroundPosition: 'center center',
          backgroundRepeat: 'no-repeat',
        }}
      >
        {/* Dark gradient overlay — keeps text legible over the image */}
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            background: `linear-gradient(105deg,
              rgba(0,32,69,0.95) 0%,
              rgba(0,32,69,0.85) 40%,
              rgba(0,32,69,0.50) 65%,
              rgba(0,32,69,0.10) 100%)`,
            pointerEvents: 'none',
          }}
        />

        <Container maxWidth="lg" sx={{ position: 'relative' }}>
          <Box sx={{ maxWidth: 560 }}>
            <Typography
              sx={{
                fontFamily: 'Inter, sans-serif',
                fontSize: { xs: 36, md: 52 },
                fontWeight: 700,
                color: '#fff',
                lineHeight: 1.1,
                letterSpacing: '-0.02em',
                mb: 2.5,
              }}
            >
              Elite Protection<br />for Your Vehicle
            </Typography>

            <Typography
              sx={{
                fontFamily: 'Inter, sans-serif',
                fontSize: { xs: 16, md: 18 },
                color: T.onPrimaryContainer,
                lineHeight: 1.6,
                mb: 4,
                maxWidth: 460,
              }}
            >
              Precision-engineered insurance for modern drivers. Manage your vehicle policies with institutional stability and digital-first transparency.
            </Typography>

            <Box sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap' }}>
              <Button
                onClick={() => navigate('/register')}
                sx={{
                  bgcolor: T.cta,
                  color: '#fff',
                  fontWeight: 700,
                  fontSize: 14,
                  px: 3,
                  py: 1.25,
                  borderRadius: '4px',
                  textTransform: 'none',
                  boxShadow: 'none',
                  '&:hover': { bgcolor: T.ctaHover, boxShadow: 'none' },
                }}
              >
                Manage Your Policy
              </Button>
              <Button
                onClick={() => navigate('/login')}
                sx={{
                  bgcolor: 'transparent',
                  border: '1px solid rgba(255,255,255,0.3)',
                  color: '#fff',
                  fontWeight: 600,
                  fontSize: 14,
                  px: 3,
                  py: 1.25,
                  borderRadius: '4px',
                  textTransform: 'none',
                  '&:hover': { bgcolor: 'rgba(255,255,255,0.06)' },
                }}
              >
                Sign In
              </Button>
            </Box>

            {/* Stats row */}
            <Box sx={{ display: 'flex', gap: 4, mt: 6, flexWrap: 'wrap' }}>
              {[
                { value: '1M+', label: 'Customers Protected' },
                { value: '2M+', label: 'Policies Issued' },
                { value: '98.5%', label: 'Claim Success Rate' },
              ].map((s) => (
                <Box key={s.label}>
                  <Typography sx={{ color: '#fff', fontWeight: 700, fontSize: 22, lineHeight: 1 }}>
                    {s.value}
                  </Typography>
                  <Typography sx={{ color: T.onPrimaryContainer, fontSize: 12, mt: 0.5 }}>
                    {s.label}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Box>
        </Container>
      </Box>

      {/* ── FEATURED POLICIES ────────────────────────────────────────────── */}
      <Container maxWidth="lg" sx={{ py: { xs: 5, md: 7 } }}>

        {/* Section header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
          <Box>
            <SectionHeading>Featured Policies</SectionHeading>
            <Typography sx={{ fontSize: 14, color: T.onSurfaceVariant, mt: 0.25 }}>
              Browse active premium coverage plans for current vehicle models.
            </Typography>
          </Box>
          <Button
            endIcon={<ChevronRightIcon sx={{ fontSize: 16 }} />}
            onClick={() => navigate('/policies')}
            sx={{
              color: T.primary,
              fontWeight: 600,
              fontSize: 13,
              textTransform: 'none',
              p: 0,
              minWidth: 0,
              '&:hover': { bgcolor: 'transparent', textDecoration: 'underline' },
            }}
          >
            View All Policies
          </Button>
        </Box>

        {/* Policy cards grid */}
        {policiesLoading ? (
          /* Skeleton placeholders while loading */
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mt: 2 }}>
            {[1, 2, 3].map((i) => (
              <Box key={i} sx={{ flex: '1 1 280px', border: `1px solid ${T.outlineVariant}`, borderRadius: '4px', p: 2.5, bgcolor: '#fff' }}>
                <Skeleton variant="rectangular" height={40} sx={{ mb: 1.5, borderRadius: '4px' }} />
                <Skeleton width="70%" height={20} sx={{ mb: 1 }} />
                <Skeleton width="40%" height={28} sx={{ mb: 1.5 }} />
                <Skeleton width="90%" height={14} sx={{ mb: 0.5 }} />
                <Skeleton width="80%" height={14} sx={{ mb: 0.5 }} />
                <Skeleton width="85%" height={14} sx={{ mb: 2 }} />
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Skeleton variant="rectangular" height={32} sx={{ flex: 1, borderRadius: '4px' }} />
                  <Skeleton variant="rectangular" height={32} sx={{ flex: 1, borderRadius: '4px' }} />
                </Box>
              </Box>
            ))}
          </Box>
        ) : featuredPolicies.length === 0 ? (
          <Box sx={{ mt: 2, p: 4, textAlign: 'center', border: `1px solid ${T.outlineVariant}`, borderRadius: '4px', bgcolor: '#fff' }}>
            <Typography sx={{ color: T.onSurfaceVariant, fontSize: 14 }}>
              No policies available at the moment.
            </Typography>
          </Box>
        ) : (
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mt: 2 }}>
            {featuredPolicies.map((p) => {
              const typeNum = p.vehicleType; // 1=Car, 2=Truck, 3=Bike
              const typeLabel = VEHICLE_LABELS[typeNum] || 'Vehicle';
              const icon = VEHICLE_ICONS[typeNum];
              const coverageItems = splitCoverage(p.coverageDetails).slice(0, 3);

              return (
                <Box
                  key={p.policyId}
                  sx={{
                    flex: '1 1 280px',
                    border: `1px solid ${T.outlineVariant}`,
                    borderRadius: '4px',
                    bgcolor: '#fff',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'box-shadow 0.15s, transform 0.12s',
                    '&:hover': {
                      boxShadow: '0 4px 20px rgba(0,32,69,0.1)',
                      transform: 'translateY(-2px)',
                    },
                  }}
                >
                  {/* Card header */}
                  <Box
                    sx={{
                      bgcolor: T.surfaceContainerLow,
                      px: 2.5,
                      py: 2,
                      borderBottom: `1px solid ${T.outlineVariant}`,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                    }}
                  >
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {icon}
                      <Typography sx={{ fontSize: 13, fontWeight: 600, color: T.onSurface }}>{p.name}</Typography>
                    </Box>
                    <Box
                      sx={{
                        px: 1.25, py: 0.25,
                        borderRadius: '9999px',
                        bgcolor: T.successBg,
                        color: T.success,
                        fontSize: 11,
                        fontWeight: 700,
                        letterSpacing: '0.05em',
                      }}
                    >
                      {typeLabel.toUpperCase()}
                    </Box>
                  </Box>

                  {/* Card body */}
                  <Box sx={{ p: 2.5, flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                    <Typography sx={{ fontSize: 11, color: T.onSurfaceVariant, mb: 0.25 }}>Annual Premium</Typography>
                    <Typography sx={{ fontSize: 22, fontWeight: 800, color: T.primary, mb: 2, lineHeight: 1 }}>
                      ₹{p.premium?.toLocaleString()}
                      <Typography component="span" sx={{ fontSize: 12, fontWeight: 400, color: T.onSurfaceVariant }}> /yr</Typography>
                    </Typography>

                    {/* Coverage list */}
                    <Box sx={{ flexGrow: 1, mb: 2 }}>
                      {coverageItems.map((c, i) => (
                        <Box key={i} sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 0.5 }}>
                          <Box sx={{ width: 5, height: 5, borderRadius: '50%', bgcolor: T.primary, flexShrink: 0 }} />
                          <Typography sx={{ fontSize: 12, color: T.onSurfaceVariant, lineHeight: 1.4 }}>{c.trim()}</Typography>
                        </Box>
                      ))}
                    </Box>

                    {/* Actions */}
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <Button
                        size="small"
                        fullWidth
                        onClick={() => navigate('/policies')}
                        sx={{
                          border: `1px solid ${T.primary}`,
                          color: T.primary,
                          fontWeight: 600,
                          fontSize: 12,
                          textTransform: 'none',
                          borderRadius: '4px',
                          py: 0.75,
                          '&:hover': { bgcolor: 'rgba(0,32,69,0.05)' },
                        }}
                      >
                        View Details
                      </Button>
                      <Button
                        size="small"
                        fullWidth
                        onClick={() => navigate('/register')}
                        sx={{
                          bgcolor: T.cta,
                          color: '#fff',
                          fontWeight: 700,
                          fontSize: 12,
                          textTransform: 'none',
                          borderRadius: '4px',
                          py: 0.75,
                          boxShadow: 'none',
                          '&:hover': { bgcolor: T.ctaHover, boxShadow: 'none' },
                        }}
                      >
                        Get Started
                      </Button>
                    </Box>
                  </Box>
                </Box>
              );
            })}
          </Box>
        )}

        {/* View all link below cards */}
        {!policiesLoading && featuredPolicies.length > 0 && (
          <Box sx={{ textAlign: 'center', mt: 3 }}>
            <Button
              endIcon={<ChevronRightIcon />}
              onClick={() => navigate('/policies')}
              sx={{
                color: T.primary,
                fontWeight: 600,
                fontSize: 14,
                textTransform: 'none',
                border: `1px solid ${T.outlineVariant}`,
                borderRadius: '4px',
                px: 3,
                py: 1,
                '&:hover': { bgcolor: T.surfaceContainerLow },
              }}
            >
              View All {policies.length} Policies
            </Button>
          </Box>
        )}
      </Container>

      {/* ── WHY CHOOSE US ────────────────────────────────────────────────────── */}
      <Box sx={{ bgcolor: T.surfaceContainerLow, py: { xs: 5, md: 7 } }}>
        <Container maxWidth="lg">
          <SectionHeading>Why Trust Guard?</SectionHeading>
          <Typography sx={{ fontSize: 14, color: T.onSurfaceVariant, mb: 4 }}>
            Institutional stability meets modern digital efficiency.
          </Typography>

          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            {[
              { icon: '🛡️', title: 'Bank-Grade Security', desc: 'End-to-end encryption and secure data handling on every transaction.' },
              { icon: '⚡', title: 'Instant Policy Activation', desc: 'Purchase and activate coverage in minutes with Razorpay-powered payments.' },
              { icon: '📋', title: 'Digital-First Claims', desc: 'Submit and track claims entirely online with real-time status updates.' },
              { icon: '🎯', title: 'Specialist Support', desc: 'Dedicated claims and support specialists assigned directly to your case.' },
            ].map((f) => (
              <Box
                key={f.title}
                sx={{
                  flex: '1 1 220px',
                  bgcolor: '#fff',
                  border: `1px solid ${T.outlineVariant}`,
                  borderRadius: '4px',
                  p: 2.5,
                }}
              >
                <Typography sx={{ fontSize: 24, mb: 1.5 }}>{f.icon}</Typography>
                <Typography sx={{ fontSize: 15, fontWeight: 600, color: T.onSurface, mb: 0.75 }}>
                  {f.title}
                </Typography>
                <Typography sx={{ fontSize: 13, color: T.onSurfaceVariant, lineHeight: 1.6 }}>
                  {f.desc}
                </Typography>
              </Box>
            ))}
          </Box>
        </Container>
      </Box>

      {/* ── CTA BANNER ───────────────────────────────────────────────────────── */}
      <Box sx={{ bgcolor: T.primary, py: { xs: 5, md: 6 } }}>
        <Container maxWidth="lg">
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 3 }}>
            <Box>
              <Typography sx={{ color: '#fff', fontSize: { xs: 22, md: 28 }, fontWeight: 700, letterSpacing: '-0.01em' }}>
                Ready to protect your vehicle?
              </Typography>
              <Typography sx={{ color: T.onPrimaryContainer, fontSize: 15, mt: 0.5 }}>
                Join over 1 million customers who trust Trust Guard.
              </Typography>
            </Box>
            <Button
              onClick={() => navigate('/register')}
              sx={{
                bgcolor: T.cta,
                color: '#fff',
                fontWeight: 700,
                fontSize: 14,
                px: 3.5,
                py: 1.5,
                borderRadius: '4px',
                textTransform: 'none',
                boxShadow: 'none',
                whiteSpace: 'nowrap',
                '&:hover': { bgcolor: T.ctaHover, boxShadow: 'none' },
              }}
            >
              Get Started Free
            </Button>
          </Box>
        </Container>
      </Box>

      {/* ── FOOTER ───────────────────────────────────────────────────────────── */}
      <Box sx={{ bgcolor: T.primary, borderTop: `1px solid rgba(255,255,255,0.08)`, pt: 5, pb: 3 }}>
        <Container maxWidth="lg">
          <Box sx={{ display: 'flex', gap: 6, flexWrap: 'wrap', mb: 5 }}>

            {/* Brand */}
            <Box sx={{ flex: '2 1 240px' }}>
              <Box sx={{ mb: 1.5 }}>
                <BrandLogo size="md" />
              </Box>
              <Typography sx={{ color: T.onPrimaryContainer, fontSize: 13, lineHeight: 1.7, maxWidth: 280 }}>
                Providing institutional stability and specialized vehicle insurance solutions since 1984. Your protection is our primary mission.
              </Typography>
            </Box>

            {/* Coverage */}
            <Box sx={{ flex: '1 1 140px' }}>
              <Typography sx={{ color: '#fff', fontWeight: 600, fontSize: 13, mb: 1.5, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
                Coverage
              </Typography>
              {['Standard Auto', 'Commercial Fleet', 'Classic Car', 'Electric Vehicle'].map((item) => (
                <Typography key={item} sx={{ color: T.onPrimaryContainer, fontSize: 13, mb: 0.75, cursor: 'pointer', '&:hover': { color: '#fff' } }}>
                  {item}
                </Typography>
              ))}
            </Box>

            {/* Company */}
            <Box sx={{ flex: '1 1 140px' }}>
              <Typography sx={{ color: '#fff', fontWeight: 600, fontSize: 13, mb: 1.5, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
                Company
              </Typography>
              {['About Us', 'Claims Center', 'Agent Portal', 'Contact Support'].map((item) => (
                <Typography key={item} sx={{ color: T.onPrimaryContainer, fontSize: 13, mb: 0.75, cursor: 'pointer', '&:hover': { color: '#fff' } }}>
                  {item}
                </Typography>
              ))}
            </Box>

            {/* Connect */}
            <Box sx={{ flex: '1 1 140px' }}>
              <Typography sx={{ color: '#fff', fontWeight: 600, fontSize: 13, mb: 1.5, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
                Connect
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                <IconButton size="small" sx={{ color: T.onPrimaryContainer, border: `1px solid rgba(255,255,255,0.15)`, borderRadius: '4px', p: 0.75, '&:hover': { color: '#fff', borderColor: 'rgba(255,255,255,0.4)' } }}>
                  <PublicIcon sx={{ fontSize: 16 }} />
                </IconButton>
                <IconButton size="small" sx={{ color: T.onPrimaryContainer, border: `1px solid rgba(255,255,255,0.15)`, borderRadius: '4px', p: 0.75, '&:hover': { color: '#fff', borderColor: 'rgba(255,255,255,0.4)' } }}>
                  <MailOutlineIcon sx={{ fontSize: 16 }} />
                </IconButton>
              </Box>
              <Typography sx={{ color: T.onPrimaryContainer, fontSize: 12 }}>
                Call us: 1-800-INSURE-NOW
              </Typography>
            </Box>
          </Box>

          <Divider sx={{ borderColor: 'rgba(255,255,255,0.1)', mb: 2.5 }} />

          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
            <Typography sx={{ color: T.onPrimaryContainer, fontSize: 12 }}>
              © 2024 Trust Guard. All rights reserved.
            </Typography>
            <Box sx={{ display: 'flex', gap: 2.5 }}>
              {['Privacy Policy', 'Terms of Service', 'Cookie Policy'].map((item) => (
                <Typography key={item} sx={{ color: T.onPrimaryContainer, fontSize: 12, cursor: 'pointer', '&:hover': { color: '#fff' } }}>
                  {item}
                </Typography>
              ))}
            </Box>
          </Box>
        </Container>
      </Box>

    </Box>
  );
}
