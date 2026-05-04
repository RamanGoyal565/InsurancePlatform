import PropTypes from 'prop-types';
import { Card, CardContent, Box, Typography, Skeleton } from '@mui/material';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';

/**
 * KPI stat card.
 * trend is only shown when explicitly passed as a non-null number.
 * Pass trend={null} (default) to hide it entirely — never show fake data.
 */
export default function StatCard({ title, value, subtitle, icon, trend, loading }) {
  // Only render trend row when we have a real computed number
  const hasTrend = typeof trend === 'number' && isFinite(trend);

  return (
    <Card>
      <CardContent sx={{ p: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="caption" color="text.secondary" fontWeight={500} noWrap>
              {title}
            </Typography>
            {loading ? (
              <Skeleton width={60} height={36} />
            ) : (
              <Typography variant="h5" fontWeight={700} mt={0.5} lineHeight={1.2}>
                {value ?? '—'}
              </Typography>
            )}
            {hasTrend && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mt: 0.5 }}>
                {trend >= 0 ? (
                  <TrendingUpIcon sx={{ fontSize: 13, color: 'success.main' }} />
                ) : (
                  <TrendingDownIcon sx={{ fontSize: 13, color: 'error.main' }} />
                )}
                <Typography variant="caption" color={trend >= 0 ? 'success.main' : 'error.main'}>
                  {Math.abs(trend).toFixed(1)}% vs last month
                </Typography>
              </Box>
            )}
            {subtitle && (
              <Typography variant="caption" color="text.secondary" display="block" mt={0.25}>
                {subtitle}
              </Typography>
            )}
          </Box>
          {icon && (
            <Box
              sx={{
                width: 40, height: 40, borderRadius: 2, flexShrink: 0, ml: 1,
                bgcolor: 'primary.50',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
              }}
            >
              {icon}
            </Box>
          )}
        </Box>
      </CardContent>
    </Card>
  );
}

StatCard.propTypes = {
  title: PropTypes.string.isRequired,
  value: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
  subtitle: PropTypes.string,
  icon: PropTypes.node,
  /** Pass a real computed number to show trend, or omit/null to hide it */
  trend: PropTypes.number,
  loading: PropTypes.bool,
};

StatCard.defaultProps = {
  trend: null,
};
