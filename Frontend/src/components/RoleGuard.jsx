import PropTypes from 'prop-types';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../app/AuthContext';
import { hasRole } from '../app/roles';

/**
 * Protects a route by role.
 * Redirects unauthenticated users to /login, unauthorized to /unauthorized.
 *
 * @param {{ roles: string[], children: React.ReactNode }} props
 */
export default function RoleGuard({ roles, children }) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  if (roles.length > 0 && !hasRole(user, roles)) {
    return <Navigate to="/unauthorized" replace />;
  }
  return children;
}

RoleGuard.propTypes = {
  roles: PropTypes.arrayOf(PropTypes.string),
  children: PropTypes.node.isRequired,
};

RoleGuard.defaultProps = {
  roles: [],
};
