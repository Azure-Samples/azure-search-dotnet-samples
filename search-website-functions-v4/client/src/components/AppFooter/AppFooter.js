import React from 'react';

import './AppFooter.css';

export default function AppFooter() {
  return (
      <footer className="footer">
        <hr />
        &copy; {new Date().getFullYear()} Microsoft
      </footer>
  );
};
