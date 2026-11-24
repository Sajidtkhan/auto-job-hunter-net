let currentJobs = [];
let activeCountryFilter = '';
let selectedJobForModal = null;

document.addEventListener('DOMContentLoaded', () => {
  loadStats();
  loadJobs();
  loadProfile();
  loadConfig();
});

// Switch Tab
function switchTab(tabName) {
  document.querySelectorAll('.tab-content').forEach(el => el.classList.remove('active'));
  document.querySelectorAll('.nav-tab').forEach(el => el.classList.remove('active'));

  document.getElementById(`tab-${tabName}`).classList.add('active');
  const clickedTab = Array.from(document.querySelectorAll('.nav-tab')).find(b => b.innerText.toLowerCase().includes(tabName));
  if (clickedTab) clickedTab.classList.add('active');
}

// Fetch Stats
async function loadStats() {
  try {
    const res = await fetch('/api/stats');
    if (!res.ok) return;
    const stats = await res.json();
    document.getElementById('statDubai').innerText = stats.dubaiJobs;
    document.getElementById('statGermany').innerText = stats.germanyJobs;
    document.getElementById('statHighMatch').innerText = stats.highMatchJobs;
    document.getElementById('statApplied').innerText = stats.appliedJobs;
  } catch (err) {
    console.error('Failed to load stats', err);
  }
}

// Fetch Jobs
async function loadJobs() {
  try {
    const status = document.getElementById('selectStatus').value;
    let url = `/api/jobs?`;
    if (activeCountryFilter) url += `country=${activeCountryFilter}&`;
    if (status) url += `status=${status}&`;

    const res = await fetch(url);
    if (!res.ok) return;
    currentJobs = await res.json();
    document.getElementById('tabJobCount').innerText = currentJobs.length;
    renderJobsTable();
  } catch (err) {
    console.error('Failed to load jobs', err);
  }
}

// Render Jobs Table
function renderJobsTable() {
  const tbody = document.getElementById('jobsTableBody');
  const search = document.getElementById('searchInput').value.toLowerCase();

  const filtered = currentJobs.filter(j => {
    if (!search) return true;
    return j.title.toLowerCase().includes(search) ||
           j.company.toLowerCase().includes(search) ||
           j.matchedKeywords.toLowerCase().includes(search) ||
           j.location.toLowerCase().includes(search);
  });

  if (filtered.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 2rem; color: #9ca3af;">No jobs matching criteria found. Click "Scan Jobs Now" to search live feeds!</td></tr>`;
    return;
  }

  tbody.innerHTML = filtered.map(job => {
    const matchClass = job.matchScore >= 70 ? 'match-high' : 'match-med';
    const isDubai = job.country.toLowerCase().includes('dubai') || job.country.toLowerCase().includes('uae');
    const countryClass = isDubai ? 'country-dubai' : 'country-germany';
    const countryFlag = isDubai ? '🇦🇪 Dubai / UAE' : '🇩🇪 Germany';

    let statusBadge = `<span class="status-badge status-discovered">${job.status}</span>`;
    if (job.status === 'Applied') {
      statusBadge = `<span class="status-badge status-applied">✓ Applied</span>`;
    } else if (job.status === 'Interview') {
      statusBadge = `<span class="status-badge status-interview">⭐ Interview</span>`;
    }

    const techTags = job.matchedKeywords
      ? job.matchedKeywords.split(',').slice(0, 4).map(k => `<span class="tech-tag">${k.trim()}</span>`).join('')
      : '';

    return `
      <tr>
        <td>
          <span class="match-badge ${matchClass}">${job.matchScore}%</span>
        </td>
        <td>
          <a href="${job.jobUrl}" target="_blank" class="job-title-link">${escapeHtml(job.title)}</a>
          <span class="job-company">${escapeHtml(job.company)} ${job.salary ? `• <strong style="color:#10b981">${escapeHtml(job.salary)}</strong>` : ''}</span>
        </td>
        <td>
          <span class="country-pill ${countryClass}">${countryFlag}</span>
          <div style="font-size:0.75rem; color:#9ca3af; margin-top:0.2rem;">${escapeHtml(job.location)}</div>
        </td>
        <td style="font-size:0.75rem; color:#9ca3af;">${escapeHtml(job.source)}</td>
        <td>${techTags}</td>
        <td>${statusBadge}</td>
        <td>
          <div class="action-btns">
            <button class="btn btn-sm btn-primary" onclick="openCoverLetterModal(${job.id})">
              ✍️ Cover Letter
            </button>
            <button class="btn btn-sm ${job.status === 'Applied' ? 'btn-secondary' : 'btn-success'}" onclick="applyToJob(${job.id})">
              ${job.status === 'Applied' ? 'Re-Apply' : '🚀 Apply'}
            </button>
          </div>
        </td>
      </tr>
    `;
  }).join('');
}

// Filter functions
function filterCountry(country) {
  activeCountryFilter = country;
  document.querySelectorAll('.filter-btn').forEach(btn => btn.classList.remove('active'));
  event.target.classList.add('active');
  loadJobs();
}

function applyFilters() {
  renderJobsTable();
}

// Trigger Scan
async function triggerScan(autoApply = false) {
  const btn = autoApply ? document.getElementById('btnScanApplyNow') : document.getElementById('btnScanNow');
  const origText = btn.innerHTML;
  btn.innerHTML = '⏳ Scanning...';
  btn.disabled = true;

  showAlert(`🔍 Searching live job sources in Dubai and Germany for .NET & SQL...`, 'info');

  try {
    const res = await fetch(`/api/jobs/scan?autoApply=${autoApply}`, { method: 'POST' });
    if (res.ok) {
      const data = await res.json();
      showAlert(`✅ Discovery complete! Found ${data.newMatchesFound} new relevant matches (Dubai: ${data.dubaiCount}, Germany: ${data.germanyCount}). ${autoApply ? `Auto-applied to ${data.autoAppliedCount} jobs!` : ''}`, 'success');
      await loadStats();
      await loadJobs();
    } else {
      showAlert('Scan encountered an issue.', 'error');
    }
  } catch (err) {
    showAlert('Error triggering scan.', 'error');
  } finally {
    btn.innerHTML = origText;
    btn.disabled = false;
  }
}

// Apply to Job
async function applyToJob(jobId) {
  try {
    const res = await fetch(`/api/jobs/${jobId}/apply`, { method: 'POST' });
    if (res.ok) {
      const data = await res.json();
      showAlert(`📨 ${data.message}`, 'success');
      loadStats();
      loadJobs();
    }
  } catch (err) {
    showAlert('Failed to dispatch application.', 'error');
  }
}

// Apply to All Matches
async function applyToAllMatches() {
  const btn = document.getElementById('btnApplyAll');
  const origText = btn.innerHTML;
  btn.innerHTML = '⏳ Applying...';
  btn.disabled = true;

  try {
    const res = await fetch('/api/jobs/apply-all', { method: 'POST' });
    if (res.ok) {
      const data = await res.json();
      showAlert(`🚀 Successfully processed applications for ${data.count} jobs in Dubai & Germany!`, 'success');
      loadStats();
      loadJobs();
    } else {
      showAlert('Failed to run batch application.', 'error');
    }
  } catch (err) {
    showAlert('Error processing batch applications.', 'error');
  } finally {
    btn.innerHTML = origText;
    btn.disabled = false;
  }
}

// Cover Letter Modal
function openCoverLetterModal(jobId) {
  const job = currentJobs.find(j => j.id === jobId);
  if (!job) return;

  selectedJobForModal = job;
  document.getElementById('modalJobTitle').innerText = `Tailored Cover Letter: ${job.title}`;
  document.getElementById('modalCompanyInfo').innerText = `${job.company} • ${job.location} (${job.country})`;
  document.getElementById('modalCoverLetterText').value = job.generatedCoverLetter || '';
  document.getElementById('coverLetterModal').classList.remove('hidden');
}

function closeModal() {
  document.getElementById('coverLetterModal').classList.add('hidden');
  selectedJobForModal = null;
}

function copyLetterToClipboard() {
  const text = document.getElementById('modalCoverLetterText').value;
  navigator.clipboard.writeText(text);
  showAlert('📋 Cover letter copied to clipboard!', 'success');
}

async function applyFromModal() {
  if (!selectedJobForModal) return;
  closeModal();
  await applyToJob(selectedJobForModal.id);
}

// Candidate Profile Form
async function loadProfile() {
  try {
    const res = await fetch('/api/profile');
    if (!res.ok) return;
    const p = await res.json();
    document.getElementById('profFullName').value = p.fullName || '';
    document.getElementById('profTitle').value = p.currentTitle || '';
    document.getElementById('profEmail').value = p.email || '';
    document.getElementById('profPhone').value = p.phone || '';
    document.getElementById('profLinkedIn').value = p.linkedinUrl || '';
    document.getElementById('profNotice').value = p.noticePeriod || '';
    document.getElementById('profLocations').value = p.targetLocations || '';
    document.getElementById('profSkills').value = p.skillsSummary || '';
    document.getElementById('profResumePath').value = p.resumePath || '';
  } catch (err) {
    console.error('Failed to load profile', err);
  }
}

async function saveProfile(e) {
  e.preventDefault();
  const profile = {
    fullName: document.getElementById('profFullName').value,
    currentTitle: document.getElementById('profTitle').value,
    email: document.getElementById('profEmail').value,
    phone: document.getElementById('profPhone').value,
    linkedinUrl: document.getElementById('profLinkedIn').value,
    noticePeriod: document.getElementById('profNotice').value,
    targetLocations: document.getElementById('profLocations').value,
    skillsSummary: document.getElementById('profSkills').value,
    resumePath: document.getElementById('profResumePath').value,
  };

  try {
    const res = await fetch('/api/profile', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(profile)
    });
    if (res.ok) {
      showAlert('💾 Profile successfully updated! Cover letters will use your new details.', 'success');
    }
  } catch (err) {
    showAlert('Failed to save profile.', 'error');
  }
}

// Config / Settings Form
async function loadConfig() {
  try {
    const res = await fetch('/api/config');
    if (!res.ok) return;
    const c = await res.json();
    document.getElementById('cfgRunHour').value = c.dailyRunHour;
    document.getElementById('cfgMinScore').value = c.minMatchScoreToNotify;
    document.getElementById('cfgAutoApply').checked = c.autoSendEmailApplication;
    document.getElementById('cfgBlacklist').value = (c.blacklistedCompanies || []).join(', ');
    document.getElementById('cfgSmtpHost').value = c.smtp?.host || '';
    document.getElementById('cfgSmtpPort').value = c.smtp?.port || 587;
    document.getElementById('cfgSmtpEmail').value = c.smtp?.senderEmail || '';
    document.getElementById('cfgTelegramEnabled').checked = c.telegram?.enabled || false;
    document.getElementById('cfgTgToken').value = c.telegram?.botToken || '';
    document.getElementById('cfgTgChatId').value = c.telegram?.chatId || '';
  } catch (err) {
    console.error('Failed to load config', err);
  }
}

async function saveConfig(e) {
  e.preventDefault();
  const config = {
    dailyRunHour: parseInt(document.getElementById('cfgRunHour').value),
    minMatchScoreToNotify: parseInt(document.getElementById('cfgMinScore').value),
    autoSendEmailApplication: document.getElementById('cfgAutoApply').checked,
    blacklistedCompanies: document.getElementById('cfgBlacklist').value
      .split(',')
      .map(s => s.trim().toLowerCase())
      .filter(s => s.length > 0),
    smtp: {
      host: document.getElementById('cfgSmtpHost').value,
      port: parseInt(document.getElementById('cfgSmtpPort').value),
      senderEmail: document.getElementById('cfgSmtpEmail').value,
      password: document.getElementById('cfgSmtpPass').value,
      enableSsl: true
    },
    telegram: {
      enabled: document.getElementById('cfgTelegramEnabled').checked,
      botToken: document.getElementById('cfgTgToken').value,
      chatId: document.getElementById('cfgTgChatId').value
    }
  };

  try {
    const res = await fetch('/api/config', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(config)
    });
    if (res.ok) {
      showAlert('⚙️ Settings saved successfully! Scheduler and notifications updated.', 'success');
    }
  } catch (err) {
    showAlert('Failed to save settings.', 'error');
  }
}

function showAlert(message, type) {
  const banner = document.getElementById('alertBanner');
  banner.innerText = message;
  banner.className = 'alert-banner';
  banner.classList.remove('hidden');
  setTimeout(() => banner.classList.add('hidden'), 7000);
}

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
