import PropTypes from 'prop-types';
import { Box, Typography } from '@mui/material';

/**
 * Trust Guard brand logo — shield SVG + wordmark.
 * Used in Sidebar, TopBar, LandingPage nav, LoginPage, and the public /policies nav.
 *
 * @param {{ size?: 'sm'|'md'|'lg', dark?: boolean, textColor?: string }} props
 */
export default function BrandLogo({ size = 'md', dark = false, textColor }) {
  const sizes = {
    sm: { icon: 26, font: 16, gap: 1 },
    md: { icon: 30, font: 18, gap: 1.25 },
    lg: { icon: 36, font: 22, gap: 1.5 },
  };
  const s = sizes[size] || sizes.md;
  const color = textColor || (dark ? '#1a1c1e' : '#ffffff');

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: s.gap, userSelect: 'none' }}>
      {/* Shield SVG icon — inline so it's always offline */}
      <Box
        component="svg"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        sx={{ width: s.icon, height: s.icon, flexShrink: 0 }}
        aria-hidden="true"
      >
        {/* Shield fill */}
        <path
          d="M12 2 L3 6.5 V12 C3 16.9 7 21.5 12 23 C17 21.5 21 16.9 21 12 V6.5 Z"
          fill={dark ? '#002045' : '#ffffff'}
        />
        {/* Checkmark */}
        <path
          d="M9 12.5 L11 14.5 L15 10.5"
          stroke={dark ? '#ffffff' : '#002045'}
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
        />
      </Box>

      {/* Wordmark */}
      <Typography
        sx={{
          color,
          fontWeight: 700,
          fontSize: s.font,
          letterSpacing: '-0.01em',
          lineHeight: 1,
          fontFamily: 'Inter, sans-serif',
        }}
      >
        Trust Guard
      </Typography>
    </Box>
  );
}

BrandLogo.propTypes = {
  size: PropTypes.oneOf(['sm', 'md', 'lg']),
  dark: PropTypes.bool,
  textColor: PropTypes.string,
};
