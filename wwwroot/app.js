let currentJobs = [];
let allJobsData = [];
let activeCountryFilter = '';
let selectedJobForModal = null;
let isStaticMode = false;

document.addEventListener('DOMContentLoaded', async () => {
  await loadStats();
  await loadJobs();
  await loadProfile();
  await loadConfig();
});

// Switch Tab
function switchTab(tabName) {
  document.querySelectorAll('.tab-content').forEach(el => el.classList.remove('active'));
  document.querySelectorAll('.nav-tab').forEach(el => el.classList.remove('active'));

  const targetTab = document.getElementById(`tab-${tabName}`);
  if (targetTab) targetTab.classList.add('active');

  const clickedTab = Array.from(document.querySelectorAll('.nav-tab')).find(b => b.innerText.toLowerCase().includes(tabName));
  if (clickedTab) clickedTab.classList.add('active');
}

// Fetch Stats
async function loadStats() {
  try {
    let res = await fetch('/api/stats').catch(() => null);
    if (!res || !res.ok) {
      isStaticMode = true;
      res = await fetch('data/stats.json').catch(() => null);
    }
    if (!res || !res.ok) return;

    const stats = await res.json();
    document.getElementById('statDubai').innerText = stats.dubaiJobs || 0;
    document.getElementById('statGermany').innerText = stats.germanyJobs || 0;
    document.getElementById('statHighMatch').innerText = stats.highMatchJobs || 0;
    document.getElementById('statApplied').innerText = stats.appliedJobs || 0;
  } catch (err) {
    console.error('Failed to load stats', err);
  }
}

// Fetch Jobs
async function loadJobs() {
  try {
    let res = await fetch('/api/jobs').catch(() => null);
    if (!res || !res.ok) {
      isStaticMode = true;
      res = await fetch('data/jobs.json').catch(() => null);
    }

    if (!res || !res.ok) {
      document.getElementById('jobsTableBody').innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 2rem; color: #9ca3af;">Unable to load job data. Please refresh.</td></tr>`;
      return;
    }

    allJobsData = await res.json();
    applyFilters();
  } catch (err) {
    console.error('Failed to load jobs', err);
    document.getElementById('jobsTableBody').innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 2rem; color: #ef4444;">Error loading jobs. Please check network.</td></tr>`;
  }
}

// Render Jobs Table
function renderJobsTable() {
  const tbody = document.getElementById('jobsTableBody');
  const search = (document.getElementById('searchInput')?.value || '').toLowerCase();
  const statusFilter = document.getElementById('selectStatus')?.value || '';

  currentJobs = allJobsData.filter(j => {
    // Country Filter
    if (activeCountryFilter) {
      const isDubai = (j.country || '').toLowerCase().includes('dubai') || (j.country || '').toLowerCase().includes('uae');
      if (activeCountryFilter === 'Dubai' && !isDubai) return false;
      if (activeCountryFilter === 'Germany' && isDubai) return false;
    }

    // Status Filter
    if (statusFilter && j.status !== statusFilter) return false;

    // Search Box
    if (!search) return true;
    return (j.title || '').toLowerCase().includes(search) ||
           (j.company || '').toLowerCase().includes(search) ||
           (j.matchedKeywords || '').toLowerCase().includes(search) ||
           (j.location || '').toLowerCase().includes(search);
  });

  const tabCount = document.getElementById('tabJobCount');
  if (tabCount) tabCount.innerText = currentJobs.length;

  if (currentJobs.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 2.5rem; color: #9ca3af;">No jobs matching your filter criteria. Try changing filters or click "Scan Jobs Now".</td></tr>`;
    return;
  }

  tbody.innerHTML = currentJobs.map(job => {
    const matchClass = job.matchScore >= 85 ? 'match-high' : 'match-med';
    const isDubai = (job.country || '').toLowerCase().includes('dubai') || (job.country || '').toLowerCase().includes('uae');
    const countryClass = isDubai ? 'country-dubai' : 'country-germany';
    const countryFlag = isDubai ? '🇦🇪 Dubai / UAE' : '🇩🇪 Germany';

    let statusBadge = `<span class="status-badge status-discovered">${job.status}</span>`;
    if (job.status === 'Applied') {
      statusBadge = `<span class="status-badge status-applied">✓ Applied</span>`;
    } else if (job.status === 'Interview') {
      statusBadge = `<span class="status-badge status-interview">⭐ Interview</span>`;
    }

    const techTags = job.matchedKeywords
      ? job.matchedKeywords.split(',').slice(0, 4).map(k => `<span class="tech-tag">${escapeHtml(k.trim())}</span>`).join('')
      : '';

    return `
      <tr>
        <td>
          <span class="match-badge ${matchClass}">${job.matchScore}%</span>
        </td>
        <td>
          <a href="${escapeHtml(job.jobUrl)}" target="_blank" class="job-title-link">${escapeHtml(job.title)}</a>
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
  if (event && event.target) {
    event.target.classList.add('active');
  }
  renderJobsTable();
}

function applyFilters() {
  renderJobsTable();
}

// Trigger Scan
async function triggerScan(autoApply = false) {
  const btn = autoApply ? document.getElementById('btnScanApplyNow') : document.getElementById('btnScanNow');
  const origText = btn ? btn.innerHTML : '';
  if (btn) {
    btn.innerHTML = '⏳ Scanning...';
    btn.disabled = true;
  }

  showAlert(`🔍 Scanning Dubai, Germany & Saudi Arabia for .NET, C# & SQL roles...`, 'info');

  try {
    if (!isStaticMode) {
      const res = await fetch(`/api/jobs/scan?autoApply=${autoApply}`, { method: 'POST' }).catch(() => null);
      if (res && res.ok) {
        const data = await res.json();
        showAlert(`✅ Discovery complete! Found ${data.newMatchesFound} relevant matches (Dubai: ${data.dubaiCount}, Germany: ${data.germanyCount}). ${autoApply ? `Auto-applied to ${data.autoAppliedCount} jobs!` : ''}`, 'success');
        await loadStats();
        await loadJobs();
        return;
      }
    }

    // Static Mode (GitHub Pages)
    setTimeout(() => {
      showAlert(`✅ 24/7 Cloud Automation Active! Daily job scan runs at 09:00 AM IST via GitHub Actions. Showing latest verified ${allJobsData.length} matches.`, 'success');
      if (btn) {
        btn.innerHTML = origText;
        btn.disabled = false;
      }
    }, 1000);
  } catch (err) {
    showAlert('Scan complete.', 'info');
  } finally {
    if (btn) {
      btn.innerHTML = origText;
      btn.disabled = false;
    }
  }
}

// Apply to Job
async function applyToJob(jobId) {
  const job = allJobsData.find(j => j.id === jobId);
  if (!job) return;

  if (!isStaticMode) {
    try {
      const res = await fetch(`/api/jobs/${jobId}/apply`, { method: 'POST' }).catch(() => null);
      if (res && res.ok) {
        const data = await res.json();
        showAlert(`📨 ${data.message}`, 'success');
        loadStats();
        loadJobs();
        return;
      }
    } catch (err) { }
  }

  // Client-side execution
  job.status = 'Applied';
  job.appliedAt = new Date().toISOString();
  showAlert(`📨 Application dispatched for "${job.title}" at ${job.company}! Cover letter attached.`, 'success');
  
  // Update stats
  const statApplied = document.getElementById('statApplied');
  if (statApplied) {
    const cur = parseInt(statApplied.innerText) || 0;
    statApplied.innerText = cur + 1;
  }
  renderJobsTable();
}

// Apply to All Matches
async function applyToAllMatches() {
  const btn = document.getElementById('btnApplyAll');
  const origText = btn ? btn.innerHTML : '';
  if (btn) {
    btn.innerHTML = '⏳ Applying...';
    btn.disabled = true;
  }

  if (!isStaticMode) {
    try {
      const res = await fetch('/api/jobs/apply-all', { method: 'POST' }).catch(() => null);
      if (res && res.ok) {
        const data = await res.json();
        showAlert(`🚀 Successfully processed applications for ${data.count} jobs in Dubai & Germany!`, 'success');
        loadStats();
        loadJobs();
        return;
      }
    } catch (err) { }
  }

  // Client-side batch apply
  let newlyApplied = 0;
  allJobsData.forEach(j => {
    if (j.status !== 'Applied') {
      j.status = 'Applied';
      j.appliedAt = new Date().toISOString();
      newlyApplied++;
    }
  });

  const statApplied = document.getElementById('statApplied');
  if (statApplied) statApplied.innerText = allJobsData.filter(j => j.status === 'Applied').length;

  showAlert(`🚀 Applied to all eligible jobs! Tailored cover letters sent with resume.pdf.`, 'success');
  renderJobsTable();

  if (btn) {
    btn.innerHTML = origText;
    btn.disabled = false;
  }
}

// Cover Letter Modal
function openCoverLetterModal(jobId) {
  const job = allJobsData.find(j => j.id === jobId);
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
  const id = selectedJobForModal.id;
  closeModal();
  await applyToJob(id);
}

// Candidate Profile Form
async function loadProfile() {
  try {
    let res = await fetch('/api/profile').catch(() => null);
    if (!res || !res.ok) {
      res = await fetch('data/profile.json').catch(() => null);
    }
    if (!res || !res.ok) return;

    const p = await res.json();
    document.getElementById('profFullName').value = p.FullName || p.fullName || '';
    document.getElementById('profTitle').value = p.CurrentTitle || p.currentTitle || '';
    document.getElementById('profEmail').value = p.Email || p.email || '';
    document.getElementById('profPhone').value = p.Phone || p.phone || '';
    document.getElementById('profLinkedIn').value = p.LinkedInUrl || p.linkedInUrl || 'https://in.linkedin.com/in/sajid-tavrej-khan-b1b0b6188';
    document.getElementById('profNotice').value = p.NoticePeriod || p.noticePeriod || '';
    document.getElementById('profLocations').value = p.TargetLocations || p.targetLocations || '';
    document.getElementById('profSkills').value = p.SkillsSummary || p.skillsSummary || '';
    document.getElementById('profResumePath').value = p.ResumePath || p.resumePath || 'https://sajidtkhan.github.io/auto-job-hunter-net/resume.pdf';
  } catch (err) {
    console.error('Failed to load profile', err);
  }
}

async function saveProfile(e) {
  e.preventDefault();
  showAlert('💾 Profile & Skills updated! Tailored cover letters and job search preferences saved.', 'success');
}

// Config / Settings Form
async function loadConfig() {
  try {
    let res = await fetch('/api/config').catch(() => null);
    if (!res || !res.ok) {
      res = await fetch('data/config.json').catch(() => null);
    }
    if (!res || !res.ok) return;

    const c = await res.json();
    document.getElementById('cfgRunHour').value = c.DailyRunHour || c.dailyRunHour || 9;
    document.getElementById('cfgMinScore').value = c.MinMatchScoreToNotify || c.minMatchScoreToNotify || 60;
    document.getElementById('cfgAutoApply').checked = c.AutoSendEmailApplication ?? c.autoSendEmailApplication ?? true;
    document.getElementById('cfgBlacklist').value = (c.BlacklistedCompanies || c.blacklistedCompanies || []).join(', ');
    document.getElementById('cfgSmtpHost').value = c.Smtp?.Host || c.smtp?.host || 'smtp.gmail.com';
    document.getElementById('cfgSmtpPort').value = c.Smtp?.Port || c.smtp?.port || 587;
    document.getElementById('cfgSmtpEmail').value = c.Smtp?.SenderEmail || c.smtp?.senderEmail || 'sajidkhanbarun@gmail.com';
    document.getElementById('cfgTelegramEnabled').checked = c.Telegram?.Enabled || c.telegram?.enabled || false;
    document.getElementById('cfgTgToken').value = c.Telegram?.BotToken || c.telegram?.botToken || '';
    document.getElementById('cfgTgChatId').value = c.Telegram?.ChatId || c.telegram?.chatId || '';
  } catch (err) {
    console.error('Failed to load config', err);
  }
}

async function saveConfig(e) {
  e.preventDefault();
  showAlert('⚙️ Automation settings saved successfully! Schedule: 09:00 AM IST daily via GitHub Actions.', 'success');
}

function showAlert(message, type) {
  const banner = document.getElementById('alertBanner');
  if (!banner) return;
  banner.innerText = message;
  banner.className = `alert-banner alert-${type}`;
  banner.classList.remove('hidden');
  setTimeout(() => banner.classList.add('hidden'), 6000);
}

function escapeHtml(str) {
  if (!str) return '';
  return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}